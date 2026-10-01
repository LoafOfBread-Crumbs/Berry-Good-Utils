using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using BerryGoodUtils.Models;
using BerryGoodUtils.Modules.Scheduling;
using BerryGoodUtils.Services;

namespace BerryGoodUtils.Modules.CustomerManagement;

public partial class CustomerManagementModule : UserControl, IUtilityModule
{
    public string ModuleName => "Customer Management";
    public string Description => "Manage customer records used across the utility suite.";
    public string Icon => "👥";
    public UserControl View => this;

    private AppData _appData;
    private Customer? _selectedCustomer;

    public CustomerManagementModule()
    {
        InitializeComponent();
        _appData = DataService.LoadAppData();
        RefreshCustomers();
        Loaded += (_, _) => RefreshData();
    }

    private void RefreshData()
    {
        _appData = DataService.LoadAppData();
        RefreshCustomers();
    }

    private void RefreshCustomers()
    {
        dgCustomers.ItemsSource = _appData.Customers.OrderBy(c => c.Name).ToList();
        _selectedCustomer = dgCustomers.SelectedItem as Customer;
        UpdateActionStates();
    }

    private void DgCustomers_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedCustomer = dgCustomers.SelectedItem as Customer;
        UpdateActionStates();
    }

    private void UpdateActionStates()
    {
        var hasSelection = _selectedCustomer != null;
        btnEdit.IsEnabled = hasSelection;
        btnDelete.IsEnabled = hasSelection;
        btnOpenFolder.IsEnabled = hasSelection;
        btnSchedule.IsEnabled = hasSelection;
    }

    private void ScheduleCustomer_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedCustomer == null)
            return;

        _appData = DataService.LoadAppData();

        // If the customer already has a schedule, edit the most recently updated one.
        var existingSchedule = _appData.Schedules
            .Where(s => s.CustomerId == _selectedCustomer.Id)
            .OrderByDescending(s => s.UpdatedAt)
            .FirstOrDefault();

        var schedule = existingSchedule ?? new CustomerSchedule
        {
            CustomerId = _selectedCustomer.Id,
            Title = $"Visit - {_selectedCustomer.Name}",
            Location = _selectedCustomer.Address
        };

        var window = new SchedulingEditWindow(schedule, _appData.Customers)
        {
            Owner = Window.GetWindow(this)
        };

        if (window.ShowDialog() != true)
            return;

        if (existingSchedule == null)
            _appData.Schedules.Add(schedule);

        schedule.UpdatedAt = DateTime.Now;
        DataService.SaveAppData(_appData);
        RefreshCustomers();
    }

    private void AddCustomer_Click(object sender, RoutedEventArgs e)
    {
        var customer = new Customer();
        var window = new CustomerEditWindow(customer)
        {
            Owner = Window.GetWindow(this)
        };

        if (window.ShowDialog() != true)
            return;

        CustomerService.GetCustomerFolderPath(customer);
        _appData.Customers.Add(customer);
        DataService.SaveAppData(_appData);
        RefreshCustomers();
        dgCustomers.SelectedItem = customer;
    }

    private void EditCustomer_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedCustomer == null)
            return;

        var window = new CustomerEditWindow(_selectedCustomer)
        {
            Owner = Window.GetWindow(this)
        };

        if (window.ShowDialog() != true)
            return;

        CustomerService.GetCustomerFolderPath(_selectedCustomer);
        DataService.SaveAppData(_appData);
        RefreshCustomers();
    }

    private void DeleteCustomer_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedCustomer == null)
            return;

        var result = MessageBox.Show($"Delete customer '{_selectedCustomer.Name}'?\n\nTheir files on disk will not be removed.",
            "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes)
            return;

        _appData.Customers.Remove(_selectedCustomer);
        DataService.SaveAppData(_appData);
        _selectedCustomer = null;
        RefreshCustomers();
    }

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedCustomer == null)
            return;

        var folder = CustomerService.GetCustomerFolderPath(_selectedCustomer);
        Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true });
    }

}
