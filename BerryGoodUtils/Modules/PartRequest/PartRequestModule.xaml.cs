using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using BerryGoodUtils.Core.Email;
using BerryGoodUtils.Models;
using BerryGoodUtils.Modules.Email;
using BerryGoodUtils.Modules.QuoteGenerator;
using BerryGoodUtils.Services;
using Microsoft.Win32;

namespace BerryGoodUtils.Modules.PartRequest;

public partial class PartRequestModule : UserControl, IUtilityModule
{
    public string ModuleName => "Part Request";
    public string Description => "Build supplier part requests and generate email-ready HTML or text.";
    public string Icon => "📦";
    public UserControl View => this;

    private readonly IEmailSender _emailSender;
    private AppData _appData;
    private ObservableCollection<PartRequestItem> _requestItems = new();
    private ObservableCollection<EmailAttachment> _attachments = new();
    private Supplier? _selectedSupplier;

    public PartRequestModule(IEmailSender emailSender)
    {
        _emailSender = emailSender;
        InitializeComponent();
        _appData = DataService.LoadAppData();
        dgItems.ItemsSource = _requestItems;
        lbAttachments.ItemsSource = _attachments;
        RefreshSupplierDropdown();
        RefreshPartsDropdown();
        Loaded += (_, _) => RefreshData();
    }

    private void RefreshData()
    {
        _appData = DataService.LoadAppData();
        RefreshPartsDropdown();
    }

    private void RefreshSupplierDropdown()
    {
        cmbSupplier.ItemsSource = _appData.Suppliers.OrderBy(s => s.Name).ToList();
        cmbSupplier.SelectedItem = _selectedSupplier;
    }

    private void CmbSupplier_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedSupplier = cmbSupplier.SelectedItem as Supplier;
    }

    private void NewSupplier_Click(object sender, RoutedEventArgs e)
    {
        var ownerWindow = Window.GetWindow(this);
        var supplier = new Supplier();
        var window = new SupplierEditWindow(supplier)
        {
            Owner = ownerWindow
        };

        if (window.ShowDialog() == true)
        {
            _appData.Suppliers.Add(supplier);
            DataService.SaveAppData(_appData);
            RefreshSupplierDropdown();
            cmbSupplier.SelectedItem = supplier;
        }
    }

    private void RefreshPartsDropdown()
    {
        cmbPartService.Items.Clear();
        foreach (var part in _appData.SavedParts)
        {
            cmbPartService.Items.Add(part.Name);
        }
    }

    private void CmbPartService_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (cmbPartService.SelectedItem is string selectedName)
        {
            var part = _appData.SavedParts.FirstOrDefault(p => p.Name == selectedName);
            if (part != null)
            {
                chkAttachReferenceImage.IsEnabled = part.HasReferenceImage;
                chkAttachReferenceImage.IsChecked = part.HasReferenceImage;
            }
        }
        else
        {
            chkAttachReferenceImage.IsEnabled = false;
            chkAttachReferenceImage.IsChecked = false;
        }
    }

    private void AddItem_Click(object sender, RoutedEventArgs e)
    {
        var partName = cmbPartService.Text?.Trim();
        if (string.IsNullOrWhiteSpace(partName))
        {
            MessageBox.Show("Please enter a part or service name.", "Missing Info", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(txtQuantity.Text, out int qty) || qty < 1)
        {
            MessageBox.Show("Please enter a valid quantity (1 or more).", "Invalid Quantity", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _requestItems.Add(new PartRequestItem
        {
            PartOrService = partName,
            Quantity = qty,
            IncludeReferenceImage = chkAttachReferenceImage.IsChecked == true
        });

        if (!_appData.SavedParts.Any(p => p.Name.Equals(partName, StringComparison.OrdinalIgnoreCase)))
        {
            _appData.SavedParts.Add(new SavedPart
            {
                Name = partName,
                Description = string.Empty,
                DefaultPrice = 0
            });
            DataService.SaveAppData(_appData);
            RefreshPartsDropdown();
        }

        cmbPartService.Text = string.Empty;
        cmbPartService.SelectedIndex = -1;
        txtQuantity.Text = "1";
        chkAttachReferenceImage.IsChecked = false;
        chkAttachReferenceImage.IsEnabled = false;
    }

    private void RemoveItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is PartRequestItem item)
        {
            _requestItems.Remove(item);
        }
    }

    private void AttachImages_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Attach Images",
            Filter = "Image files|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.webp|All files|*.*",
            Multiselect = true
        };

        if (dialog.ShowDialog(Window.GetWindow(this)) != true)
            return;

        foreach (var filePath in dialog.FileNames)
            TryAddImageAttachment(filePath);
    }

    private void DropZone_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop) && e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Any(IsImageFile))
        {
            e.Effects = DragDropEffects.Copy;
            dropZone.Background = (System.Windows.Media.Brush)FindResource("SurfaceBrush");
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void DropZone_DragLeave(object sender, DragEventArgs e)
    {
        dropZone.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#f8fafc")!;
        e.Handled = true;
    }

    private void DropZone_Drop(object sender, DragEventArgs e)
    {
        dropZone.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#f8fafc")!;
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            return;

        var files = e.Data.GetData(DataFormats.FileDrop) as string[] ?? [];
        foreach (var filePath in files)
            TryAddImageAttachment(filePath);
        e.Handled = true;
    }

    private void TryAddImageAttachment(string filePath)
    {
        if (!IsImageFile(filePath))
        {
            MessageBox.Show($"'{Path.GetFileName(filePath)}' is not a supported image file.", "Invalid Attachment",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_attachments.Any(a => a.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase)))
            return;

        _attachments.Add(new EmailAttachment
        {
            FileName = Path.GetFileName(filePath),
            FilePath = filePath,
            ContentType = GetImageMimeType(filePath)
        });
    }

    private static bool IsImageFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension is ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp" or ".webp";
    }

    private static string GetImageMimeType(string filePath)
    {
        return Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }

    private void RemoveAttachment_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is EmailAttachment attachment)
            _attachments.Remove(attachment);
    }

    private void ClearRequest_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show("Clear all requested items and attachments?", "Confirm Clear",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            _requestItems.Clear();
            _attachments.Clear();
            cmbPartService.Text = string.Empty;
            cmbPartService.SelectedIndex = -1;
            txtQuantity.Text = "1";
            chkAttachReferenceImage.IsChecked = false;
            chkAttachReferenceImage.IsEnabled = false;
        }
    }

    private void SaveHtml_Click(object sender, RoutedEventArgs e)
    {
        SaveRequest(true);
    }

    private void SaveText_Click(object sender, RoutedEventArgs e)
    {
        SaveRequest(false);
    }

    private void EmailRequest_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateRequest())
            return;

        var allAttachments = GetAllAttachments();
        var message = EmailMessageFactory.ForPartRequest(_selectedSupplier!, _requestItems, _appData.Company, allAttachments, _appData.SavedParts);
        new EmailPreviewWindow(message, _emailSender, _appData,
            () => EmailMessageFactory.ForPartRequest(_selectedSupplier!, _requestItems, _appData.Company, GetAllAttachments(), _appData.SavedParts))
        { Owner = Window.GetWindow(this) }.ShowDialog();
    }

    private List<EmailAttachment> GetAllAttachments()
    {
        var allAttachments = new List<EmailAttachment>(_attachments);
        foreach (var item in _requestItems.Where(i => i.IncludeReferenceImage))
        {
            var part = _appData.SavedParts.FirstOrDefault(p => p.Name.Equals(item.PartOrService, StringComparison.OrdinalIgnoreCase));
            if (part?.HasReferenceImage != true)
                continue;

            var path = part.ReferenceImagePath;
            if (allAttachments.Any(a => a.FilePath.Equals(path, StringComparison.OrdinalIgnoreCase)))
                continue;

            allAttachments.Add(new EmailAttachment
            {
                FileName = Path.GetFileName(path),
                FilePath = path,
                ContentType = GetImageMimeType(path)
            });
        }

        return allAttachments;
    }

    private void SaveRequest(bool generateHtml)
    {
        if (!ValidateRequest())
            return;

        try
        {
            var filePath = generateHtml
                ? PartRequestExportService.SaveHtml(_selectedSupplier!, _requestItems, _appData.Company)
                : PartRequestExportService.SaveText(_selectedSupplier!, _requestItems, _appData.Company);
            MessageBox.Show($"Request saved!\n\nFile: {filePath}\n\nOpening for review/copy-paste...",
                "Request Generated", MessageBoxButton.OK, MessageBoxImage.Information);
            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error generating request: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private bool ValidateRequest()
    {
        if (_selectedSupplier == null)
        {
            MessageBox.Show("Please select or create a supplier first.", "Missing Info", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
        if (_requestItems.Count == 0)
        {
            MessageBox.Show("Please add at least one item to the request.", "No Items", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
        return true;
    }

    private void CompanySettings_Click(object sender, RoutedEventArgs e)
    {
        var ownerWindow = Window.GetWindow(this);
        var settingsWindow = new CompanySettingsWindow(_appData)
        {
            Owner = ownerWindow
        };

        if (settingsWindow.ShowDialog() == true)
        {
            _appData = DataService.LoadAppData();
            _selectedSupplier = _appData.Suppliers.FirstOrDefault(s => s.Id == _selectedSupplier?.Id);
            RefreshSupplierDropdown();
            RefreshPartsDropdown();
        }
    }
}
