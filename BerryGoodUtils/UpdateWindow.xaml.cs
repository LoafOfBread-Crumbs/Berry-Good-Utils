using System.Windows;
using BerryGoodUtils.Services;

namespace BerryGoodUtils;

public partial class UpdateWindow : Window
{
    private readonly UpdateCheckResult _result;

    public UpdateWindow(UpdateCheckResult result)
    {
        InitializeComponent();
        _result = result;

        tbCurrentVersion.Text = _result.CurrentVersion.ToString();
        tbLatestVersion.Text = _result.LatestVersion.ToString();
        tbReleaseNotes.Text = string.IsNullOrWhiteSpace(_result.ReleaseNotes)
            ? "No release notes provided."
            : _result.ReleaseNotes;

        btnDownload.IsEnabled = !string.IsNullOrWhiteSpace(_result.DownloadUrl);
    }

    private async void Download_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_result.DownloadUrl))
        {
            MessageBox.Show("No downloadable asset was found for this release.", "Update", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        btnDownload.IsEnabled = false;
        progressBar.Visibility = Visibility.Visible;
        tbStatus.Visibility = Visibility.Visible;
        tbStatus.Text = "Downloading update...";

        try
        {
            var progress = new Progress<double>(value => progressBar.Value = value);
            var downloadedPath = await UpdateService.DownloadUpdateAsync(_result.DownloadUrl, progress);

            tbStatus.Text = "Installing update...";
            UpdateService.ApplyUpdate(downloadedPath);

            MessageBox.Show(
                "The update has been downloaded and will be applied when the application restarts.",
                "Update",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            btnDownload.IsEnabled = true;
            progressBar.Visibility = Visibility.Collapsed;
            tbStatus.Text = $"Download failed: {ex.Message}";
            MessageBox.Show($"Could not download or install the update:\n\n{ex.Message}", "Update Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Later_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
