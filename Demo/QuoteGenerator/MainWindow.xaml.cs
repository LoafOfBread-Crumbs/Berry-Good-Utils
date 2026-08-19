using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using QuoteGenerator.Models;
using QuoteGenerator.Services;

namespace QuoteGenerator;

public partial class MainWindow : Window
{
    private AppData _appData;
    private ObservableCollection<QuoteItem> _quoteItems = new();

    public MainWindow()
    {
        InitializeComponent();
        _appData = DataService.LoadAppData();
        dgItems.ItemsSource = _quoteItems;
        RefreshPartsDropdown();
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
                txtDescription.Text = part.Description;
                txtUnitPrice.Text = part.DefaultPrice.ToString("F2");
            }
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

        if (!decimal.TryParse(txtUnitPrice.Text, out decimal price) || price < 0)
        {
            MessageBox.Show("Please enter a valid unit price.", "Invalid Price", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var item = new QuoteItem
        {
            PartOrService = partName,
            Description = txtDescription.Text?.Trim() ?? string.Empty,
            Quantity = qty,
            UnitPrice = price
        };

        _quoteItems.Add(item);
        UpdateTotal();

        // Save part for future quick access if it doesn't already exist
        if (!_appData.SavedParts.Any(p => p.Name.Equals(partName, StringComparison.OrdinalIgnoreCase)))
        {
            _appData.SavedParts.Add(new SavedPart
            {
                Name = partName,
                Description = txtDescription.Text?.Trim() ?? string.Empty,
                DefaultPrice = price
            });
            DataService.SaveAppData(_appData);
            RefreshPartsDropdown();
        }

        // Clear input fields
        cmbPartService.Text = string.Empty;
        cmbPartService.SelectedIndex = -1;
        txtDescription.Text = string.Empty;
        txtQuantity.Text = "1";
        txtUnitPrice.Text = "0.00";
    }

    private void RemoveItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is QuoteItem item)
        {
            _quoteItems.Remove(item);
            UpdateTotal();
        }
    }

    private void UpdateTotal()
    {
        var total = _quoteItems.Sum(i => i.Total);
        txtTotal.Text = total.ToString("C2");
    }

    private void ClearQuote_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show("Clear all fields and start a new quote?", "Confirm Clear",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            txtCustomerName.Text = string.Empty;
            txtCustomerEmail.Text = string.Empty;
            txtCustomerPhone.Text = string.Empty;
            txtCustomerAddress.Text = string.Empty;
            txtNotes.Text = string.Empty;
            _quoteItems.Clear();
            UpdateTotal();
        }
    }

    private void GenerateQuote_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtCustomerName.Text))
        {
            MessageBox.Show("Please enter a customer name.", "Missing Info", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_quoteItems.Count == 0)
        {
            MessageBox.Show("Please add at least one item to the quote.", "No Items", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var quote = new Quote
        {
            QuoteNumber = DataService.GetNextQuoteNumber(_appData),
            Date = DateTime.Now,
            CustomerName = txtCustomerName.Text.Trim(),
            CustomerEmail = txtCustomerEmail.Text?.Trim() ?? string.Empty,
            CustomerPhone = txtCustomerPhone.Text?.Trim() ?? string.Empty,
            CustomerAddress = txtCustomerAddress.Text?.Trim() ?? string.Empty,
            Notes = txtNotes.Text?.Trim() ?? string.Empty,
            Items = new ObservableCollection<QuoteItem>(_quoteItems)
        };

        try
        {
            var filePath = QuoteExportService.SaveQuoteHtml(quote, _appData.Company);
            MessageBox.Show($"Quote {quote.QuoteNumber} saved!\n\nFile: {filePath}\n\nOpening in browser for preview/print...",
                "Quote Generated", MessageBoxButton.OK, MessageBoxImage.Information);

            // Open in default browser so user can print to PDF
            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error generating quote: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CompanySettings_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new CompanySettingsWindow(_appData);
        settingsWindow.Owner = this;
        if (settingsWindow.ShowDialog() == true)
        {
            _appData = DataService.LoadAppData();
        }
    }
}
