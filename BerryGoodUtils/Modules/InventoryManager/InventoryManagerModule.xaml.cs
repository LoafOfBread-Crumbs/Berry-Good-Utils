using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using BerryGoodUtils.Core.Documents;
using BerryGoodUtils.Core.Ocr;
using BerryGoodUtils.Models;
using BerryGoodUtils.Services;
using BerryGoodUtils.Services.Ocr;
using Microsoft.Win32;
using System.Windows.Input;

namespace BerryGoodUtils.Modules.InventoryManager;

public partial class InventoryManagerModule : UserControl, IUtilityModule
{
    public string ModuleName => "Inventory Manager";
    public string Description => "Add, edit, and remove parts from the shared inventory.";
    public string Icon => "🗂️";
    public UserControl View => this;

    private AppData _appData;
    private SavedPart? _selectedPart;
    private string? _pendingImagePath;
    private readonly List<CheckBox> _emailFieldCheckBoxes = [];

    public InventoryManagerModule()
    {
        InitializeComponent();
        BuildEmailFieldCheckBoxes();
        _appData = DataService.LoadAppData();
        dgParts.ItemsSource = _appData.SavedParts;
        Loaded += (_, _) => RefreshData();
    }

    private void RefreshData()
    {
        _appData = DataService.LoadAppData();
        dgParts.ItemsSource = _appData.SavedParts;
        _selectedPart = dgParts.SelectedItem as SavedPart;
    }

    private void BuildEmailFieldCheckBoxes()
    {
        foreach (var (name, label) in PartDetailFormatter.GetFields())
        {
            var checkBox = new CheckBox
            {
                Content = label,
                Tag = name,
                Margin = new Thickness(0, 0, 12, 6),
                FontSize = 12,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            _emailFieldCheckBoxes.Add(checkBox);
            wpEmailFields.Children.Add(checkBox);
        }
    }

    private void DgParts_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedPart = dgParts.SelectedItem as SavedPart;
        LoadPartIntoForm(_selectedPart);
    }

    private void NewPart_Click(object sender, RoutedEventArgs e)
    {
        dgParts.SelectedItem = null;
        _selectedPart = null;
        ClearForm();
        txtName.Focus();
    }

    private void DeleteSelectedPart_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedPart == null)
        {
            MessageBox.Show("Please select a part to delete.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var result = MessageBox.Show($"Delete '{_selectedPart.Name}' from inventory?", "Confirm Delete",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes)
            return;

        _appData.SavedParts.Remove(_selectedPart);
        DataService.SaveAppData(_appData);
        _selectedPart = null;
        ClearForm();
    }

    private void RemovePart_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is SavedPart part)
        {
            var result = MessageBox.Show($"Delete '{part.Name}' from inventory?", "Confirm Delete",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
                return;

            if (_selectedPart == part)
            {
                _selectedPart = null;
                ClearForm();
            }

            _appData.SavedParts.Remove(part);
            DataService.SaveAppData(_appData);
        }
    }

    private void SavePart_Click(object sender, RoutedEventArgs e)
    {
        var name = txtName.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Part name is required.", "Missing Info", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!decimal.TryParse(txtDefaultPrice.Text, out var defaultPrice))
            defaultPrice = 0;

        var isNew = _selectedPart == null;
        var existingByName = _appData.SavedParts.FirstOrDefault(p =>
            p.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && p != _selectedPart);

        if (existingByName != null && isNew)
        {
            var overwrite = MessageBox.Show($"A part named '{name}' already exists. Update it instead?", "Duplicate Part",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (overwrite == MessageBoxResult.Yes)
            {
                _selectedPart = existingByName;
                dgParts.SelectedItem = _selectedPart;
                isNew = false;
            }
            else
            {
                return;
            }
        }

        var part = _selectedPart ?? new SavedPart();
        part.Name = name;
        part.Manufacturer = txtManufacturer.Text?.Trim() ?? string.Empty;
        part.EquipmentType = txtEquipmentType.Text?.Trim() ?? string.Empty;
        part.PartNumber = txtPartNumber.Text?.Trim() ?? string.Empty;
        part.ModelNumber = txtModelNumber.Text?.Trim() ?? string.Empty;
        part.SerialNumber = txtSerialNumber.Text?.Trim() ?? string.Empty;
        part.ReferenceNumber = txtReferenceNumber.Text?.Trim() ?? string.Empty;
        part.AdditionalIdentifier = txtAdditionalIdentifier.Text?.Trim() ?? string.Empty;
        part.Description = txtDescription.Text?.Trim() ?? string.Empty;
        part.DefaultPrice = defaultPrice;
        part.Voltage = txtVoltage.Text?.Trim() ?? string.Empty;
        part.Amps = txtAmps.Text?.Trim() ?? string.Empty;
        part.Frequency = txtFrequency.Text?.Trim() ?? string.Empty;
        part.Phase = txtPhase.Text?.Trim() ?? string.Empty;
        part.Horsepower = txtHorsepower.Text?.Trim() ?? string.Empty;
        part.Kilowatts = txtKilowatts.Text?.Trim() ?? string.Empty;
        part.IPRating = txtIPRating.Text?.Trim() ?? string.Empty;
        part.Refrigerant = txtRefrigerant.Text?.Trim() ?? string.Empty;
        part.RefrigerantCharge = txtRefrigerantCharge.Text?.Trim() ?? string.Empty;
        part.MaxCellPressure = txtMaxCellPressure.Text?.Trim() ?? string.Empty;
        part.MaxPressure = txtMaxPressure.Text?.Trim() ?? string.Empty;
        part.MaxHead = txtMaxHead.Text?.Trim() ?? string.Empty;
        part.TargetOutput = txtTargetOutput.Text?.Trim() ?? string.Empty;
        part.GrossWeight = txtGrossWeight.Text?.Trim() ?? string.Empty;
        part.ApprovalNumber = txtApprovalNumber.Text?.Trim() ?? string.Empty;
        part.BuildDate = txtBuildDate.Text?.Trim() ?? string.Empty;
        part.Barcode = txtBarcode.Text?.Trim() ?? string.Empty;
        part.CountryOfManufacture = txtCountryOfManufacture.Text?.Trim() ?? string.Empty;
        part.Notes = txtNotes.Text?.Trim() ?? string.Empty;

        part.IncludedEmailFields = _emailFieldCheckBoxes
            .Where(cb => cb.IsChecked == true)
            .Select(cb => (string)cb.Tag)
            .ToList();
        PartDetailFormatter.AutoIncludePopulatedFields(part);

        if (!string.IsNullOrWhiteSpace(_pendingImagePath))
        {
            part.ReferenceImagePath = CopyImageToDataFolder(_pendingImagePath);
            _pendingImagePath = null;
        }

        if (isNew)
            _appData.SavedParts.Add(part);

        DataService.SaveAppData(_appData);
        _selectedPart = part;
        dgParts.ItemsSource = _appData.SavedParts;
        dgParts.SelectedItem = part;

        MessageBox.Show("Part saved.", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void AddPartFromImage_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select Reference Image",
            Filter = "Image files|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.webp|All files|*.*",
            Multiselect = false
        };

        if (dialog.ShowDialog(Window.GetWindow(this)) != true)
            return;

        var sourcePath = dialog.FileName;
        var ownerWindow = Window.GetWindow(this);
        var originalTitle = ownerWindow?.Title;
        SetBusy(true);
        try
        {
            var progress = new Progress<string>(status =>
            {
                if (ownerWindow != null)
                    ownerWindow.Title = $"Berry Good Utils - {status}";
            });
            var ocrText = await OcrEngineService.ExtractTextAsync(sourcePath, progress);
            var parsedPart = PartNameplateParser.Parse(ocrText);

            if (string.IsNullOrWhiteSpace(parsedPart.Name))
                parsedPart.Name = Path.GetFileNameWithoutExtension(sourcePath);

            var destPath = CopyImageToDataFolder(sourcePath);
            parsedPart.ReferenceImagePath = destPath;

            var reviewWindow = new PartFromImageWindow(destPath, parsedPart, ocrText)
            {
                Owner = Window.GetWindow(this)
            };

            if (reviewWindow.ShowDialog() != true)
                return;

            var part = reviewWindow.Part!;
            part.ReferenceImagePath = destPath;

            var existing = _appData.SavedParts.FirstOrDefault(p =>
                p.Name.Equals(part.Name, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                var overwrite = MessageBox.Show($"A part named '{part.Name}' already exists. Replace it?", "Duplicate Part",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (overwrite != MessageBoxResult.Yes)
                    return;

                _appData.SavedParts.Remove(existing);
            }

            _appData.SavedParts.Add(part);
            DataService.SaveAppData(_appData);
            dgParts.ItemsSource = _appData.SavedParts;
            dgParts.SelectedItem = part;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not extract part details from the image:\n\n{ex.Message}", "OCR Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            if (ownerWindow != null && originalTitle != null)
                ownerWindow.Title = originalTitle;
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        IsEnabled = !busy;
        Mouse.OverrideCursor = busy ? Cursors.Wait : null;
    }

    private void BrowseImage_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select Reference Image",
            Filter = "Image files|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.webp|All files|*.*",
            Multiselect = false
        };

        if (dialog.ShowDialog(Window.GetWindow(this)) != true)
            return;

        _pendingImagePath = dialog.FileName;
        LoadImagePreview(_pendingImagePath);
        txtImagePath.Text = Path.GetFileName(_pendingImagePath);
    }

    private void ClearImage_Click(object sender, RoutedEventArgs e)
    {
        _pendingImagePath = null;
        if (_selectedPart != null)
            _selectedPart.ReferenceImagePath = string.Empty;
        ClearImagePreview();
        DataService.SaveAppData(_appData);
    }

    private void EmailFields_CheckAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var cb in _emailFieldCheckBoxes)
            cb.IsChecked = true;
    }

    private void EmailFields_UncheckAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var cb in _emailFieldCheckBoxes)
            cb.IsChecked = false;
    }

    private void LoadPartIntoForm(SavedPart? part)
    {
        if (part == null)
        {
            ClearForm();
            return;
        }

        txtName.Text = part.Name;
        txtManufacturer.Text = part.Manufacturer;
        txtEquipmentType.Text = part.EquipmentType;
        txtPartNumber.Text = part.PartNumber;
        txtModelNumber.Text = part.ModelNumber;
        txtSerialNumber.Text = part.SerialNumber;
        txtReferenceNumber.Text = part.ReferenceNumber;
        txtAdditionalIdentifier.Text = part.AdditionalIdentifier;
        txtDescription.Text = part.Description;
        txtDefaultPrice.Text = part.DefaultPrice.ToString("F2");
        txtVoltage.Text = part.Voltage;
        txtAmps.Text = part.Amps;
        txtFrequency.Text = part.Frequency;
        txtPhase.Text = part.Phase;
        txtHorsepower.Text = part.Horsepower;
        txtKilowatts.Text = part.Kilowatts;
        txtIPRating.Text = part.IPRating;
        txtRefrigerant.Text = part.Refrigerant;
        txtRefrigerantCharge.Text = part.RefrigerantCharge;
        txtMaxCellPressure.Text = part.MaxCellPressure;
        txtMaxPressure.Text = part.MaxPressure;
        txtMaxHead.Text = part.MaxHead;
        txtTargetOutput.Text = part.TargetOutput;
        txtGrossWeight.Text = part.GrossWeight;
        txtApprovalNumber.Text = part.ApprovalNumber;
        txtBuildDate.Text = part.BuildDate;
        txtBarcode.Text = part.Barcode;
        txtCountryOfManufacture.Text = part.CountryOfManufacture;
        txtNotes.Text = part.Notes;
        _pendingImagePath = null;

        var includedFields = part.IncludedEmailFields ?? [];
        foreach (var cb in _emailFieldCheckBoxes)
            cb.IsChecked = includedFields.Contains((string)cb.Tag);

        if (!string.IsNullOrWhiteSpace(part.ReferenceImagePath) && File.Exists(part.ReferenceImagePath))
        {
            LoadImagePreview(part.ReferenceImagePath);
            txtImagePath.Text = Path.GetFileName(part.ReferenceImagePath);
        }
        else
        {
            ClearImagePreview();
        }
    }

    private void ClearForm()
    {
        txtName.Text = string.Empty;
        txtManufacturer.Text = string.Empty;
        txtEquipmentType.Text = string.Empty;
        txtPartNumber.Text = string.Empty;
        txtModelNumber.Text = string.Empty;
        txtSerialNumber.Text = string.Empty;
        txtReferenceNumber.Text = string.Empty;
        txtAdditionalIdentifier.Text = string.Empty;
        txtDescription.Text = string.Empty;
        txtDefaultPrice.Text = "0.00";
        txtVoltage.Text = string.Empty;
        txtAmps.Text = string.Empty;
        txtFrequency.Text = string.Empty;
        txtPhase.Text = string.Empty;
        txtHorsepower.Text = string.Empty;
        txtKilowatts.Text = string.Empty;
        txtIPRating.Text = string.Empty;
        txtRefrigerant.Text = string.Empty;
        txtRefrigerantCharge.Text = string.Empty;
        txtMaxCellPressure.Text = string.Empty;
        txtMaxPressure.Text = string.Empty;
        txtMaxHead.Text = string.Empty;
        txtTargetOutput.Text = string.Empty;
        txtGrossWeight.Text = string.Empty;
        txtApprovalNumber.Text = string.Empty;
        txtBuildDate.Text = string.Empty;
        txtBarcode.Text = string.Empty;
        txtCountryOfManufacture.Text = string.Empty;
        txtNotes.Text = string.Empty;
        _pendingImagePath = null;
        ClearImagePreview();

        foreach (var cb in _emailFieldCheckBoxes)
            cb.IsChecked = false;
    }

    private void LoadImagePreview(string path)
    {
        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(path);
            bitmap.EndInit();
            imgReference.Source = bitmap;
        }
        catch
        {
            imgReference.Source = null;
        }
    }

    private void ClearImagePreview()
    {
        imgReference.Source = null;
        txtImagePath.Text = string.Empty;
    }

    private static string CopyImageToDataFolder(string sourcePath)
    {
        var folder = DataService.GetPartImagesFolder();
        var extension = Path.GetExtension(sourcePath);
        var safeName = $"{Guid.NewGuid():N}{extension}";
        var destPath = Path.Combine(folder, safeName);
        File.Copy(sourcePath, destPath, overwrite: true);
        return destPath;
    }
}
