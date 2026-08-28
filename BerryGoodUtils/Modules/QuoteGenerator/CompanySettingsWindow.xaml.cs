using System.Windows;
using BerryGoodUtils.Models;
using BerryGoodUtils.Services;

namespace BerryGoodUtils.Modules.QuoteGenerator;

public partial class CompanySettingsWindow : Window
{
    private readonly AppData _appData;

    public CompanySettingsWindow(AppData appData)
    {
        InitializeComponent();
        _appData = appData;

        txtCompanyName.Text = _appData.Company.CompanyName;
        txtCompanyAddress.Text = _appData.Company.Address;
        txtCompanyPhone.Text = _appData.Company.Phone;
        txtCompanyEmail.Text = _appData.Company.Email;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        _appData.Company.CompanyName = txtCompanyName.Text?.Trim() ?? string.Empty;
        _appData.Company.Address = txtCompanyAddress.Text?.Trim() ?? string.Empty;
        _appData.Company.Phone = txtCompanyPhone.Text?.Trim() ?? string.Empty;
        _appData.Company.Email = txtCompanyEmail.Text?.Trim() ?? string.Empty;

        DataService.SaveAppData(_appData);
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
