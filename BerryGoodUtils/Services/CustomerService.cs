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

    public static string GetQuotesFolder(Customer customer)
    {
        var customerFolder = GetCustomerFolderPath(customer);
        var quotesFolder = Path.Combine(customerFolder, "Quotes");
        if (!Directory.Exists(quotesFolder))
            Directory.CreateDirectory(quotesFolder);
        return quotesFolder;
    }

    public static void OpenRootFolder()
    {
        var folder = GetRootFolder();
        Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true });
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
