using System.IO;
using System.Windows;

namespace BerryGoodUtils.ClarkeeMode;

public partial class ClarkeeVideoWindow : Window
{
    private readonly string _videoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Clarkees", "clarkee-mode-finale.mp4");
    private bool _isPlaying;

    public ClarkeeVideoWindow()
    {
        InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (!File.Exists(_videoPath))
            return;

        placeholderDetail.Text = "Loading the finale...";
        videoPlayer.Source = new Uri(_videoPath, UriKind.Absolute);
        videoPlayer.Play();
    }

    private void VideoPlayer_MediaOpened(object sender, RoutedEventArgs e)
    {
        placeholderPanel.Visibility = Visibility.Collapsed;
        btnPlayPause.IsEnabled = true;
        btnPlayPause.Content = "Pause";
        _isPlaying = true;
    }

    private void VideoPlayer_MediaEnded(object sender, RoutedEventArgs e)
    {
        videoPlayer.Stop();
        btnPlayPause.Content = "Play again";
        _isPlaying = false;
    }

    private void VideoPlayer_MediaFailed(object sender, ExceptionRoutedEventArgs e)
    {
        videoPlayer.Stop();
        placeholderPanel.Visibility = Visibility.Visible;
        placeholderDetail.Text = "The finale video could not be played yet.";
        btnPlayPause.IsEnabled = false;
        _isPlaying = false;
    }

    private void PlayPause_Click(object sender, RoutedEventArgs e)
    {
        if (_isPlaying)
        {
            videoPlayer.Pause();
            btnPlayPause.Content = "Play";
        }
        else
        {
            videoPlayer.Play();
            btnPlayPause.Content = "Pause";
        }

        _isPlaying = !_isPlaying;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Window_Closed(object? sender, EventArgs e)
    {
        videoPlayer.Stop();
        videoPlayer.Source = null;
    }
}
