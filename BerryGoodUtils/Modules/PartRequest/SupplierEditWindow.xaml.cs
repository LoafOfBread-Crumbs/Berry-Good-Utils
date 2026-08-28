using System.Windows;
using BerryGoodUtils.Models;

namespace BerryGoodUtils.Modules.PartRequest;

public partial class SupplierEditWindow : Window
{
    public Supplier Supplier { get; }

    public SupplierEditWindow(Supplier supplier)
    {
        InitializeComponent();
        Supplier = supplier;

        tbTitle.Text = string.IsNullOrWhiteSpace(supplier.Name) ? "New Supplier" : "Edit Supplier";
        txtSupplierName.Text = supplier.Name;
        txtCompany.Text = supplier.Company;
        txtPhone.Text = supplier.Phone;
        txtEmail.Text = supplier.Email;
        txtAddress.Text = supplier.Address;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var name = txtSupplierName.Text?.Trim() ?? string.Empty;
        var company = txtCompany.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(company))
        {
            MessageBox.Show("Please enter a supplier name or company.", "Missing Info", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Supplier.Name = string.IsNullOrWhiteSpace(name) ? company : name;
        Supplier.Company = company;
        Supplier.Phone = txtPhone.Text?.Trim() ?? string.Empty;
        Supplier.Email = txtEmail.Text?.Trim() ?? string.Empty;
        Supplier.Address = txtAddress.Text?.Trim() ?? string.Empty;

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
