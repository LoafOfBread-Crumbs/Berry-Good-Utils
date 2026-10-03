using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BerryGoodUtils.Models;

namespace BerryGoodUtils.Services;

public static class CustomerService
{
    private static readonly string RootFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "BerryGoodUtils",
        "Customers");

    public static string GetRootFolder()
    {
        if (!Directory.Exists(RootFolder))
            Directory.CreateDirectory(RootFolder);
        return RootFolder;
    }

    public static string GetCustomerFolderPath(Customer customer)
    {
        if (customer == null)
            throw new ArgumentNullException(nameof(customer));
        if (string.IsNullOrWhiteSpace(customer.Name))
            throw new ArgumentException("Customer name is required.", nameof(customer));

        var safeName = MakeSafeFolderName(customer.Name);
        var path = Path.Combine(GetRootFolder(), safeName);
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
        return path;
    }

    public static string GetVisitPhotosFolder(Customer customer, DateTime occurrence)
    {
        var path = Path.Combine(GetCustomerFolderPath(customer), "Customer Files", occurrence.ToString("yyyy-MM-dd"));
        Directory.CreateDirectory(path);
        return path;
    }

    public static string CopyVisitPhoto(Customer customer, DateTime occurrence, string sourcePath)
    {
        var folder = GetVisitPhotosFolder(customer, occurrence);
        var fileName = MakeSafeFileName(Path.GetFileName(sourcePath));
        var destination = GetUniqueFilePath(folder, fileName);
        File.Copy(sourcePath, destination);
        return destination;
    }

    public static string SaveDownloadedVisitPhoto(Customer customer, DateTime occurrence, string fileName, Stream content)
    {
        var folder = GetVisitPhotosFolder(customer, occurrence);
        var destination = GetUniqueFilePath(folder, MakeSafeFileName(fileName));
        using var output = File.Create(destination);
        content.CopyTo(output);
        return destination;
    }

    public static string SaveVisitCommentsSnapshot(Customer customer, DateTime occurrence, string comments)
    {
        var folder = GetVisitPhotosFolder(customer, occurrence);
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss-fff");
        var path = Path.Combine(folder, $"Post-Visit Comments {timestamp}.txt");
        File.WriteAllText(path, comments ?? string.Empty);
        return path;
    }

    public static void OpenVisitPhotosFolder(Customer customer, DateTime occurrence)
    {
        var folder = GetVisitPhotosFolder(customer, occurrence);
        Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true });
    }

    public static void OpenRootFolder()
    {
        var folder = GetRootFolder();
        Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true });
    }

    private static string GetUniqueFilePath(string folder, string fileName)
    {
        var path = Path.Combine(folder, fileName);
        if (!File.Exists(path))
            return path;

        var stem = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        for (var number = 2; ; number++)
        {
            path = Path.Combine(folder, $"{stem} ({number}){extension}");
            if (!File.Exists(path))
                return path;
        }
    }

    private static string MakeSafeFileName(string name)
    {
        var safe = string.Join("_", name.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).Trim();
        return string.IsNullOrWhiteSpace(safe) ? "Photo" : safe;
    }

    private static string MakeSafeFolderName(string name)
    {
        var safe = string.Join(
            "_",
            name.Trim().Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries))
            .Trim();

        if (string.IsNullOrWhiteSpace(safe))
            safe = "Customer";

        return safe;
    }
}
