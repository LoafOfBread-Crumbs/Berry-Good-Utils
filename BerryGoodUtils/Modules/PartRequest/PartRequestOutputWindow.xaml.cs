using System.Windows;

namespace BerryGoodUtils.Modules.PartRequest;

public partial class PartRequestOutputWindow : Window
{
    public bool GenerateHtml { get; private set; }

    public PartRequestOutputWindow()
    {
        InitializeComponent();
    }

    private void Html_Click(object sender, RoutedEventArgs e)
    {
        GenerateHtml = true;
        DialogResult = true;
        Close();
    }

    private void Text_Click(object sender, RoutedEventArgs e)
    {
        GenerateHtml = false;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
