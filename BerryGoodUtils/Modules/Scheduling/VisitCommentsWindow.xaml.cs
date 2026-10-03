using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using BerryGoodUtils.Models;
using BerryGoodUtils.Services;
using Microsoft.Win32;

namespace BerryGoodUtils.Modules.Scheduling;

public partial class VisitCommentsWindow : Window
{
    private readonly Customer? _customer;
    private readonly DateTime _occurrence;
    private readonly string _originalComments;
    private readonly ObservableCollection<VisitPhoto> _photos;
    private readonly List<string> _pendingPhotoPaths = [];

    public string ResultComments { get; private set; }
    public List<VisitPhoto> ResultPhotos => _photos.ToList();

    public VisitCommentsWindow(CustomerSchedule schedule, Customer? customer, DateTime occurrence, string comments, IEnumerable<VisitPhoto>? photos = null)
    {
        InitializeComponent();
        _customer = customer;
        _occurrence = occurrence;
        _originalComments = comments;
        ResultComments = comments;
        _photos = new ObservableCollection<VisitPhoto>(photos ?? []);
        lbPhotos.ItemsSource = _photos;
        tbSchedule.Text = schedule.Title;
        tbCustomer.Text = customer?.Name ?? "Unknown customer";
        tbOccurrence.Text = occurrence.ToString("dddd, dd MMMM yyyy 'at' hh:mm tt");
        txtComments.Text = comments;
        txtComments.Focus();
        txtComments.CaretIndex = txtComments.Text.Length;
    }

    private void AddPictures_Click(object sender, RoutedEventArgs e)
    {
        if (_customer == null)
        {
            MessageBox.Show("A customer is required before pictures can be added.", "Customer Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = "Select visit pictures",
            Filter = "Image files|*.jpg;*.jpeg;*.png;*.gif;*.webp;*.heic|All files|*.*",
            Multiselect = true
        };
        if (dialog.ShowDialog() != true)
            return;

        foreach (var path in dialog.FileNames.Where(path => !_pendingPhotoPaths.Contains(path, StringComparer.OrdinalIgnoreCase)))
        {
            _pendingPhotoPaths.Add(path);
            _photos.Add(new VisitPhoto { FileName = Path.GetFileName(path), LocalPath = path, AddedAt = DateTime.Now });
        }
    }

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        if (_customer != null)
            CustomerService.OpenVisitPhotosFolder(_customer, _occurrence);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        ResultComments = txtComments.Text?.Trim() ?? string.Empty;
        if (_customer != null)
        {
            foreach (var sourcePath in _pendingPhotoPaths)
            {
                var photo = _photos.First(p => string.Equals(p.LocalPath, sourcePath, StringComparison.OrdinalIgnoreCase));
                photo.LocalPath = CustomerService.CopyVisitPhoto(_customer, _occurrence, sourcePath);
                photo.FileName = Path.GetFileName(photo.LocalPath);
                photo.MimeType = GetImageMimeType(photo.LocalPath);
            }
            if (!string.Equals(ResultComments, _originalComments, StringComparison.Ordinal))
                CustomerService.SaveVisitCommentsSnapshot(_customer, _occurrence, ResultComments);
        }
        DialogResult = true;
        Close();
    }

    private static string GetImageMimeType(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".gif" => "image/gif",
        ".webp" => "image/webp",
        ".heic" => "image/heic",
        _ => "image/jpeg"
    };

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
