using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using BerryGoodUtils.ClarkeeMode;
using BerryGoodUtils.Modules;
using BerryGoodUtils.Services;

namespace BerryGoodUtils;

public partial class MainWindow : Window
{
    private const double ClarkeeSize = 48;
    private static readonly string[] ClarkeeImages =
    {
        "/Assets/Clarkees/clarkee.png",
        "/Assets/Clarkees/clarkee2.png",
        "/Assets/Clarkees/clarkee fear me.png",
        "/Assets/Clarkees/cursed spa clarkee.png"
    };

    private bool _clarkeeHuntActive;
    private int _clarkeesFound;

    public MainWindow()
    {
        InitializeComponent();
        LoadDashboardTiles();
        clarkeeCanvas.SizeChanged += ClarkeeCanvas_SizeChanged;
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
        ClearClarkeeHunt();
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

    private void ClarkeeMode_Click(object sender, RoutedEventArgs e)
    {
        ClearClarkeeHunt();
        _clarkeeHuntActive = true;
        _clarkeesFound = 0;
        Dispatcher.BeginInvoke(PlaceClarkees, DispatcherPriority.Loaded);
    }

    private void ClarkeeCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_clarkeeHuntActive && clarkeeCanvas.Children.Count > 0)
            PlaceClarkees();
    }

    private void PlaceClarkees()
    {
        if (!_clarkeeHuntActive || clarkeeCanvas.ActualWidth <= 0 || clarkeeCanvas.ActualHeight <= 0)
            return;

        var remainingImages = clarkeeCanvas.Children
            .OfType<Button>()
            .Select(button => (string)button.Tag)
            .ToList();
        if (remainingImages.Count == 0 && _clarkeesFound == 0)
            remainingImages.AddRange(ClarkeeImages);

        clarkeeCanvas.Children.Clear();
        var targetSize = Math.Min(ClarkeeSize, Math.Max(40, (Math.Min(clarkeeCanvas.ActualWidth, clarkeeCanvas.ActualHeight) - 30) / 2));
        var positions = CreateClarkeePositions(remainingImages.Count, targetSize);

        for (var index = 0; index < remainingImages.Count; index++)
        {
            var imagePath = remainingImages[index];
            var image = new Image
            {
                Source = new BitmapImage(new Uri(imagePath, UriKind.Relative)),
                Stretch = Stretch.Uniform,
                IsHitTestVisible = false
            };
            var button = new Button
            {
                Width = targetSize,
                Height = targetSize,
                Padding = new Thickness(0),
                BorderThickness = new Thickness(0),
                Background = Brushes.Transparent,
                Cursor = System.Windows.Input.Cursors.Hand,
                Content = image,
                Tag = imagePath,
                ToolTip = "You found a Clarkee"
            };
            button.Click += Clarkee_Click;
            Canvas.SetLeft(button, positions[index].X);
            Canvas.SetTop(button, positions[index].Y);
            clarkeeCanvas.Children.Add(button);
        }
    }

    private List<Point> CreateClarkeePositions(int count, double targetSize)
    {
        const double margin = 8;
        const double spacing = 18;
        var availableWidth = Math.Max(0, clarkeeCanvas.ActualWidth - margin * 2);
        var availableHeight = Math.Max(0, clarkeeCanvas.ActualHeight - margin * 2);
        var cellWidth = availableWidth / 2;
        var cellHeight = availableHeight / 2;
        var cells = Enumerable.Range(0, 4).OrderBy(_ => Random.Shared.Next()).Take(count).ToList();
        var positions = new List<Point>();
        var occupied = new List<Rect>();

        foreach (var cell in cells)
        {
            var column = cell % 2;
            var row = cell / 2;
            var minX = margin + column * cellWidth;
            var minY = margin + row * cellHeight;
            var maxX = Math.Max(minX, minX + cellWidth - targetSize);
            var maxY = Math.Max(minY, minY + cellHeight - targetSize);
            Point? position = null;

            for (var attempt = 0; attempt < 80; attempt++)
            {
                var candidate = new Point(
                    minX + Random.Shared.NextDouble() * Math.Max(0, maxX - minX),
                    minY + Random.Shared.NextDouble() * Math.Max(0, maxY - minY));
                var bounds = new Rect(candidate.X - spacing, candidate.Y - spacing, targetSize + spacing * 2, targetSize + spacing * 2);
                if (occupied.All(existing => !existing.IntersectsWith(bounds)))
                {
                    position = candidate;
                    occupied.Add(bounds);
                    break;
                }
            }

            positions.Add(position ?? new Point((minX + maxX) / 2, (minY + maxY) / 2));
        }

        return positions;
    }

    private void Clarkee_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || !_clarkeeHuntActive)
            return;

        clarkeeCanvas.Children.Remove(button);
        _clarkeesFound++;
        if (_clarkeesFound < ClarkeeImages.Length)
            return;

        ClearClarkeeHunt();
        new ClarkeeVideoWindow { Owner = this }.ShowDialog();
    }

    private void ClearClarkeeHunt()
    {
        _clarkeeHuntActive = false;
        _clarkeesFound = 0;
        clarkeeCanvas.Children.Clear();
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

    private async void Update_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var result = await UpdateService.CheckForUpdateAsync();

            if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
            {
                MessageBox.Show(result.ErrorMessage, "Update Check Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!result.IsUpdateAvailable)
            {
                MessageBox.Show($"You are running the latest version ({result.CurrentVersion}).", "No Updates Available", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            new UpdateWindow(result) { Owner = this }.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not check for updates: {ex.Message}", "Update Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
