using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BerryGoodUtils.Modules;
using BerryGoodUtils.Services;

namespace BerryGoodUtils;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        LoadDashboardTiles();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var workArea = SystemParameters.WorkArea;

        if (ActualWidth > workArea.Width)
        {
            MaxWidth = workArea.Width;
            Width = workArea.Width;
        }

        if (ActualHeight > workArea.Height)
        {
            MaxHeight = workArea.Height;
            Height = workArea.Height;
        }

        Left = Math.Max(workArea.Left, Math.Min(Left, workArea.Right - ActualWidth));
        Top = Math.Max(workArea.Top, Math.Min(Top, workArea.Bottom - ActualHeight));
    }

    private void LoadDashboardTiles()
    {
        modulesGrid.Children.Clear();

        foreach (var module in ModuleRegistry.Modules)
        {
            var tile = CreateTile(module);
            modulesGrid.Children.Add(tile);
        }
    }

    private Button CreateTile(IUtilityModule module)
    {
        var titleBlock = new TextBlock
        {
            Text = $"{module.Icon} {module.ModuleName}",
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Colors.Black),
            Margin = new Thickness(0, 0, 0, 6)
        };

        var descBlock = new TextBlock
        {
            Text = module.Description,
            FontSize = 12,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748b")),
            TextWrapping = TextWrapping.Wrap
        };

        var panel = new StackPanel();
        panel.Children.Add(titleBlock);
        panel.Children.Add(descBlock);

        var button = new Button
        {
            Style = (Style)FindResource("DashboardTile"),
            Content = panel,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            VerticalContentAlignment = VerticalAlignment.Center
        };

        button.Click += (s, e) => LaunchModule(module);
        return button;
    }

    private void LaunchModule(IUtilityModule module)
    {
        tbModuleTitle.Text = $"{module.Icon} {module.ModuleName}";
        moduleHost.Content = module.View;

        dashboardPanel.Visibility = Visibility.Collapsed;
        moduleHost.Visibility = Visibility.Visible;
        moduleTitleBar.Visibility = Visibility.Visible;
        btnBack.Visibility = Visibility.Visible;
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        // Clear the active module and return to dashboard
        if (moduleHost.Content is FrameworkElement view)
        {
            // If the module supports deactivation, call it. Currently optional.
        }

        moduleHost.Content = null;
        moduleHost.Visibility = Visibility.Collapsed;
        moduleTitleBar.Visibility = Visibility.Collapsed;
        btnBack.Visibility = Visibility.Collapsed;
        dashboardPanel.Visibility = Visibility.Visible;
    }

    private void OpenCustomerFolders_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            CustomerService.OpenRootFolder();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not open customer folders: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
