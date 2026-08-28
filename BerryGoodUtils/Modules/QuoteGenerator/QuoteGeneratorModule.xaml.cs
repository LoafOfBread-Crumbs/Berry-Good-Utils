using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using BerryGoodUtils.Models;
using BerryGoodUtils.Services;

namespace BerryGoodUtils.Modules.QuoteGenerator;

public partial class QuoteGeneratorModule : UserControl, IUtilityModule
{
    public string ModuleName => "Quote Generator";
    public string Description => "Create customer quotes, add line items, and export to HTML/PDF.";
    public string Icon => "📝";
    public UserControl View => this;

    private AppData _appData;
    private ObservableCollection<QuoteItem> _quoteItems = new();
    private Customer? _selectedCustomer;

    public QuoteGeneratorModule()
    {
        InitializeComponent();
        _appData = DataService.LoadAppData();
        dgItems.ItemsSource = _quoteItems;
        RefreshPartsDropdown();
        RefreshCustomerDropdown();
    }

    private void RefreshCustomerDropdown()
    {
        cmbCustomer.ItemsSource = _appData.Customers.OrderBy(c => c.Name).ToList();
        cmbCustomer.SelectedItem = _selectedCustomer;
    }

    private void CmbCustomer_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (cmbCustomer.SelectedItem is Customer customer)
        {
            _selectedCustomer = customer;
            txtCustomerEmail.Text = customer.Email;
            txtCustomerPhone.Text = customer.Phone;
            txtCustomerAddress.Text = customer.Address;
        }
        else
        {
            _selectedCustomer = null;
            txtCustomerEmail.Text = string.Empty;
            txtCustomerPhone.Text = string.Empty;
            txtCustomerAddress.Text = string.Empty;
        }
    }

    private void NewCustomer_Click(object sender, RoutedEventArgs e)
    {
        var ownerWindow = Window.GetWindow(this);
        var customer = new Customer();
        var window = new CustomerEditWindow(customer)
        {
            Owner = ownerWindow
        };

        if (window.ShowDialog() == true)
        {
            CustomerService.GetCustomerFolderPath(customer);
            _appData.Customers.Add(customer);
            DataService.SaveAppData(_appData);
            RefreshCustomerDropdown();
            cmbCustomer.SelectedItem = customer;
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
        var result = MessageBox.Show("Clear the quote items and notes?", "Confirm Clear",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            txtNotes.Text = string.Empty;
            _quoteItems.Clear();
            UpdateTotal();

            cmbPartService.Text = string.Empty;
            cmbPartService.SelectedIndex = -1;
            txtDescription.Text = string.Empty;
            txtQuantity.Text = "1";
            txtUnitPrice.Text = "0.00";
        }
    }

    private void GenerateQuote_Click(object sender, RoutedEventArgs e)
    {
        if (_quoteItems.Count == 0)
        {
            MessageBox.Show("Please add at least one item to the quote.", "No Items", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var ownerWindow = Window.GetWindow(this);
        var destinationWindow = new QuoteDestinationWindow
        {
            Owner = ownerWindow
        };

        if (destinationWindow.ShowDialog() != true)
            return;

        string targetFolder;
        string quoteCustomerName;
        string customerEmail = string.Empty;
        string customerPhone = string.Empty;
        string customerAddress = string.Empty;

        if (destinationWindow.IsBusinessQuote)
        {
            targetFolder = BusinessService.GetQuotesFolder();
            quoteCustomerName = "Business";
        }
        else
        {
            if (_selectedCustomer == null)
            {
                MessageBox.Show("Please select or create a customer first.", "Missing Info", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _selectedCustomer.Email = txtCustomerEmail.Text?.Trim() ?? string.Empty;
            _selectedCustomer.Phone = txtCustomerPhone.Text?.Trim() ?? string.Empty;
            _selectedCustomer.Address = txtCustomerAddress.Text?.Trim() ?? string.Empty;
            DataService.SaveAppData(_appData);

            targetFolder = CustomerService.GetQuotesFolder(_selectedCustomer);
            quoteCustomerName = _selectedCustomer.Name;
            customerEmail = _selectedCustomer.Email;
            customerPhone = _selectedCustomer.Phone;
            customerAddress = _selectedCustomer.Address;
        }

        var quote = new Quote
        {
            QuoteNumber = DataService.GetNextQuoteNumber(_appData),
            Date = DateTime.Now,
            CustomerName = quoteCustomerName,
            CustomerEmail = customerEmail,
            CustomerPhone = customerPhone,
            CustomerAddress = customerAddress,
            Notes = txtNotes.Text?.Trim() ?? string.Empty,
            Items = new ObservableCollection<QuoteItem>(_quoteItems)
        };

        try
        {
            var filePath = QuoteExportService.SaveQuoteHtml(quote, _appData.Company, targetFolder);
            MessageBox.Show($"Quote {quote.QuoteNumber} saved!\n\nFile: {filePath}\n\nOpening in browser for preview/print...",
                "Quote Generated", MessageBoxButton.OK, MessageBoxImage.Information);

            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error generating quote: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
            _selectedCustomer = _appData.Customers.FirstOrDefault(c => c.Id == _selectedCustomer?.Id);
            RefreshCustomerDropdown();
            RefreshPartsDropdown();
        }
    }
}
