using System.Windows;
using BerryGoodUtils.Models;

namespace BerryGoodUtils.Modules.QuoteGenerator;

public partial class CustomerEditWindow : Window
{
    public Customer Customer { get; }

    public CustomerEditWindow(Customer customer)
    {
        InitializeComponent();
        Customer = customer;

        tbTitle.Text = string.IsNullOrWhiteSpace(customer.Name) ? "New Customer" : "Edit Customer";
        txtCustomerName.Text = customer.Name;
        txtCustomerPhone.Text = customer.Phone;
        txtCustomerEmail.Text = customer.Email;
        txtCustomerAddress.Text = customer.Address;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var name = txtCustomerName.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Please enter a customer name.", "Missing Info", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Customer.Name = name;
        Customer.Phone = txtCustomerPhone.Text?.Trim() ?? string.Empty;
        Customer.Email = txtCustomerEmail.Text?.Trim() ?? string.Empty;
        Customer.Address = txtCustomerAddress.Text?.Trim() ?? string.Empty;

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
