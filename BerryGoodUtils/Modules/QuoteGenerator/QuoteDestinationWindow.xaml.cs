using System.Windows;

namespace BerryGoodUtils.Modules.QuoteGenerator;

public partial class QuoteDestinationWindow : Window
{
    public bool IsBusinessQuote { get; private set; }

    public QuoteDestinationWindow()
    {
        InitializeComponent();
    }

    private void CustomerQuote_Click(object sender, RoutedEventArgs e)
    {
        IsBusinessQuote = false;
        DialogResult = true;
        Close();
    }

    private void BusinessQuote_Click(object sender, RoutedEventArgs e)
    {
        IsBusinessQuote = true;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
