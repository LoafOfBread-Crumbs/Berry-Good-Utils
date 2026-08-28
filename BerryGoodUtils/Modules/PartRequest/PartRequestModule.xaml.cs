using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using BerryGoodUtils.Models;
using BerryGoodUtils.Modules.QuoteGenerator;
using BerryGoodUtils.Services;

namespace BerryGoodUtils.Modules.PartRequest;

public partial class PartRequestModule : UserControl, IUtilityModule
{
    public string ModuleName => "Part Request";
    public string Description => "Build supplier part requests and generate email-ready HTML or text.";
    public string Icon => "📦";
    public UserControl View => this;

    private AppData _appData;
    private ObservableCollection<PartRequestItem> _requestItems = new();
    private Supplier? _selectedSupplier;

    public PartRequestModule()
    {
        InitializeComponent();
        _appData = DataService.LoadAppData();
        dgItems.ItemsSource = _requestItems;
        RefreshSupplierDropdown();
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
            Quantity = qty
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
    }

    private void RemoveItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is PartRequestItem item)
        {
            _requestItems.Remove(item);
        }
    }

    private void ClearRequest_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show("Clear all requested items?", "Confirm Clear",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            _requestItems.Clear();
            cmbPartService.Text = string.Empty;
            cmbPartService.SelectedIndex = -1;
            txtQuantity.Text = "1";
        }
    }

    private void GenerateRequest_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedSupplier == null)
        {
            MessageBox.Show("Please select or create a supplier first.", "Missing Info", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_requestItems.Count == 0)
        {
            MessageBox.Show("Please add at least one item to the request.", "No Items", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var ownerWindow = Window.GetWindow(this);
        var outputWindow = new PartRequestOutputWindow
        {
            Owner = ownerWindow
        };

        if (outputWindow.ShowDialog() != true)
            return;

        try
        {
            string filePath;
            if (outputWindow.GenerateHtml)
            {
                filePath = PartRequestExportService.SaveHtml(_selectedSupplier, _requestItems, _appData.Company);
            }
            else
            {
                filePath = PartRequestExportService.SaveText(_selectedSupplier, _requestItems, _appData.Company);
            }

            MessageBox.Show($"Request saved!\n\nFile: {filePath}\n\nOpening for review/copy-paste...",
                "Request Generated", MessageBoxButton.OK, MessageBoxImage.Information);

            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error generating request: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
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
