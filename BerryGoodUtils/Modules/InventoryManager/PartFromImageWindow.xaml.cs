using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using BerryGoodUtils.Core.Ocr;
using BerryGoodUtils.Models;
using BerryGoodUtils.Services.Ocr;

namespace BerryGoodUtils.Modules.InventoryManager;

public partial class PartFromImageWindow : Window
{
    public SavedPart? Part { get; private set; }
    private readonly string _imagePath;

    public PartFromImageWindow(string imagePath, SavedPart parsedPart, string ocrText)
    {
        InitializeComponent();
        _imagePath = imagePath;
        LoadImagePreview(imagePath);
        txtRawOcr.Text = ocrText;
        PopulateForm(parsedPart);
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // Ensure the window fits on small laptop screens with the taskbar accounted for.
        var workArea = SystemParameters.WorkArea;
        MaxHeight = workArea.Height - 40;
        MaxWidth = workArea.Width - 40;

        if (Height > MaxHeight)
            Height = MaxHeight;
        if (Width > MaxWidth)
            Width = MaxWidth;
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
            imgPreview.Source = bitmap;
        }
        catch
        {
            imgPreview.Source = null;
        }
    }

    private void PopulateForm(SavedPart part)
    {
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
    }

    private SavedPart BuildPartFromForm()
    {
        var part = new SavedPart();
        if (!decimal.TryParse(txtDefaultPrice.Text, out var defaultPrice))
            defaultPrice = 0;

        part.Name = txtName.Text?.Trim() ?? string.Empty;
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
        part.ReferenceImagePath = _imagePath;

        return part;
    }

    private async void ReRunOcr_Click(object sender, RoutedEventArgs e)
    {
        SetBusy(true);
        try
        {
            var ocrText = await OcrEngineService.ExtractTextAsync(_imagePath, new Progress<string>(status => Title = $"Add Part from Image - {status}"));
            txtRawOcr.Text = ocrText;
            PopulateForm(PartNameplateParser.Parse(ocrText));
            Title = "Add Part from Image";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"OCR failed:\n\n{ex.Message}", "OCR Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void AddPart_Click(object sender, RoutedEventArgs e)
    {
        var part = BuildPartFromForm();
        if (string.IsNullOrWhiteSpace(part.Name))
        {
            MessageBox.Show("Please enter a part name.", "Missing Info", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Part = part;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void SetBusy(bool busy)
    {
        IsEnabled = !busy;
        Mouse.OverrideCursor = busy ? Cursors.Wait : null;
    }
}
