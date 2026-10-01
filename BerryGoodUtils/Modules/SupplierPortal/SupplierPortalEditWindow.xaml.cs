using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using BerryGoodUtils.Models;

namespace BerryGoodUtils.Modules.SupplierPortal;

public partial class SupplierPortalEditWindow : Window
{
    public Supplier Supplier { get; }
    private readonly Supplier _originalSupplier;
    private readonly ObservableCollection<SupplierPortalLink> _portalLinks = new();

    public SupplierPortalEditWindow(Supplier supplier)
    {
        InitializeComponent();
        _originalSupplier = supplier;

        // Work on a copy so cancellation does not mutate the shared object.
        Supplier = new Supplier
        {
            Id = supplier.Id,
            Name = supplier.Name,
            Company = supplier.Company,
            Email = supplier.Email,
            Phone = supplier.Phone,
            Address = supplier.Address,
            CreatedAt = supplier.CreatedAt
        };

        foreach (var link in supplier.PortalLinks ?? [])
        {
            var linkCopy = new SupplierPortalLink
            {
                Id = link.Id,
                Name = link.Name,
                Url = link.Url,
                Notes = link.Notes,
                CreatedAt = link.CreatedAt
            };
            Supplier.PortalLinks.Add(linkCopy);
            _portalLinks.Add(linkCopy);
        }

        tbTitle.Text = string.IsNullOrWhiteSpace(supplier.Name) ? "New Supplier" : "Edit Supplier";
        txtSupplierName.Text = Supplier.Name;
        txtCompany.Text = Supplier.Company;
        txtPhone.Text = Supplier.Phone;
        txtEmail.Text = Supplier.Email;
        txtAddress.Text = Supplier.Address;
        icPortalLinks.ItemsSource = _portalLinks;
    }

    private void AddLink_Click(object sender, RoutedEventArgs e)
    {
        var link = new SupplierPortalLink();
        Supplier.PortalLinks.Add(link);
        _portalLinks.Add(link);
    }

    private void RemoveLink_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not SupplierPortalLink link)
            return;

        Supplier.PortalLinks.Remove(link);
        _portalLinks.Remove(link);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var name = txtSupplierName.Text?.Trim() ?? string.Empty;
        var company = txtCompany.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(company))
        {
            MessageBox.Show("Please enter a supplier name or company.", "Missing Info",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Copy values back to the original shared supplier.
        _originalSupplier.Name = string.IsNullOrWhiteSpace(name) ? company : name;
        _originalSupplier.Company = company;
        _originalSupplier.Phone = txtPhone.Text?.Trim() ?? string.Empty;
        _originalSupplier.Email = txtEmail.Text?.Trim() ?? string.Empty;
        _originalSupplier.Address = txtAddress.Text?.Trim() ?? string.Empty;
        _originalSupplier.PortalLinks = Supplier.PortalLinks.ToList();

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
