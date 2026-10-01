using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BerryGoodUtils.Models;
using BerryGoodUtils.Services;

namespace BerryGoodUtils.Modules.SupplierPortal;

public partial class SupplierPortalModule : UserControl, IUtilityModule
{
    public string ModuleName => "Supplier Portal";
    public string Description => "Save supplier websites and open them from one place.";
    public string Icon => "🌐";
    public UserControl View => this;

    private AppData _appData;
    private Supplier? _selectedSupplier;

    public SupplierPortalModule()
    {
        InitializeComponent();
        _appData = DataService.LoadAppData();
        RefreshSuppliers();
        Loaded += (_, _) => RefreshData();
    }

    private void RefreshData()
    {
        _appData = DataService.LoadAppData();
        RefreshSuppliers();
    }

    private void RefreshSuppliers()
    {
        dgSuppliers.ItemsSource = _appData.Suppliers.OrderBy(s => s.Name).ToList();
        _selectedSupplier = dgSuppliers.SelectedItem as Supplier;
        UpdateActionStates();
        RefreshSupplierDetails();
    }

    private void DgSuppliers_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedSupplier = dgSuppliers.SelectedItem as Supplier;
        UpdateActionStates();
        RefreshSupplierDetails();
    }

    private void UpdateActionStates()
    {
        var hasSelection = _selectedSupplier != null;
        btnEditSupplier.IsEnabled = hasSelection;
        btnDeleteSupplier.IsEnabled = hasSelection;
    }

    private void RefreshSupplierDetails()
    {
        wpPortalLinks.Children.Clear();

        if (_selectedSupplier == null)
        {
            tbSelectedSupplier.Text = "Select a supplier to view portals";
            gridSupplierDetails.Visibility = Visibility.Collapsed;
            tbNoLinks.Visibility = Visibility.Collapsed;
            return;
        }

        gridSupplierDetails.Visibility = Visibility.Visible;

        var displayName = string.IsNullOrWhiteSpace(_selectedSupplier.Name)
            ? _selectedSupplier.Company
            : _selectedSupplier.Name;
        tbSelectedSupplier.Text = displayName;

        tbPhone.Text = string.IsNullOrWhiteSpace(_selectedSupplier.Phone) ? "-" : _selectedSupplier.Phone;
        tbEmail.Text = string.IsNullOrWhiteSpace(_selectedSupplier.Email) ? "-" : _selectedSupplier.Email;
        tbAddress.Text = string.IsNullOrWhiteSpace(_selectedSupplier.Address) ? "-" : _selectedSupplier.Address;

        var links = _selectedSupplier.PortalLinks ?? [];
        if (links.Count == 0)
        {
            tbNoLinks.Visibility = Visibility.Visible;
            return;
        }

        tbNoLinks.Visibility = Visibility.Collapsed;
        foreach (var link in links)
        {
            var button = new Button
            {
                Content = string.IsNullOrWhiteSpace(link.Name) ? link.Url : link.Name,
                Tag = link,
                Margin = new Thickness(0, 0, 8, 8),
                Padding = new Thickness(10, 5, 10, 5),
                Background = (SolidColorBrush)FindResource("BerryBlueBrush"),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontSize = 13
            };
            button.Click += PortalLink_Click;
            wpPortalLinks.Children.Add(button);
        }
    }

    private void PortalLink_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not SupplierPortalLink link)
            return;

        var url = NormalizeUrl(link.Url);
        if (string.IsNullOrWhiteSpace(url))
        {
            MessageBox.Show("This portal link does not have a valid URL.", "Invalid Link", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not open the default browser:\n\n{ex.Message}", "Browser Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void AddSupplier_Click(object sender, RoutedEventArgs e)
    {
        var supplier = new Supplier();
        var window = new SupplierPortalEditWindow(supplier)
        {
            Owner = Window.GetWindow(this)
        };

        if (window.ShowDialog() != true)
            return;

        _appData.Suppliers.Add(supplier);
        DataService.SaveAppData(_appData);
        RefreshSuppliers();
        dgSuppliers.SelectedItem = supplier;
    }

    private void EditSupplier_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedSupplier == null)
            return;

        var window = new SupplierPortalEditWindow(_selectedSupplier)
        {
            Owner = Window.GetWindow(this)
        };

        if (window.ShowDialog() != true)
            return;

        DataService.SaveAppData(_appData);
        RefreshSuppliers();
    }

    private void DeleteSupplier_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedSupplier == null)
            return;

        var displayName = string.IsNullOrWhiteSpace(_selectedSupplier.Name)
            ? _selectedSupplier.Company
            : _selectedSupplier.Name;

        var result = MessageBox.Show($"Delete supplier '{displayName}'?\n\nTheir portal links will also be removed.",
            "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes)
            return;

        _appData.Suppliers.Remove(_selectedSupplier);
        DataService.SaveAppData(_appData);
        _selectedSupplier = null;
        RefreshSuppliers();
    }

    private static string NormalizeUrl(string url)
    {
        var trimmed = url?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmed))
            return string.Empty;

        if (!trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = "https://" + trimmed;
        }

        return Uri.TryCreate(trimmed, UriKind.Absolute, out _) ? trimmed : string.Empty;
    }
}
