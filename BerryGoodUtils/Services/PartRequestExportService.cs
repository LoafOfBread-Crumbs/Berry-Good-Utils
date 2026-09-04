using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using BerryGoodUtils.Core.Documents;
using BerryGoodUtils.Models;

namespace BerryGoodUtils.Services;

public static class PartRequestExportService
{
    public static string GenerateHtml(Supplier supplier, ObservableCollection<PartRequestItem> items, CompanyInfo company)
    {
        return PartRequestDocumentComposer.GenerateHtml(supplier, items, company);
    }

    public static string GenerateText(Supplier supplier, ObservableCollection<PartRequestItem> items, CompanyInfo company)
    {
        return PartRequestDocumentComposer.GenerateText(supplier, items, company);
    }

    public static string SaveHtml(Supplier supplier, ObservableCollection<PartRequestItem> items, CompanyInfo company)
    {
        var html = GenerateHtml(supplier, items, company);
        var folder = BusinessService.GetPartRequestsFolder();
        var supplierName = GetSupplierName(supplier).Replace(" ", "_");
        var filename = $"PartRequest_{supplierName}_{DateTime.Now:yyyyMMdd}.html";
        foreach (var c in Path.GetInvalidFileNameChars())
            filename = filename.Replace(c, '_');
        var path = Path.Combine(folder, filename);
        File.WriteAllText(path, html, Encoding.UTF8);
        return path;
    }

    public static string SaveText(Supplier supplier, ObservableCollection<PartRequestItem> items, CompanyInfo company)
    {
        var text = GenerateText(supplier, items, company);
        var folder = BusinessService.GetPartRequestsFolder();
        var supplierName = GetSupplierName(supplier).Replace(" ", "_");
        var filename = $"PartRequest_{supplierName}_{DateTime.Now:yyyyMMdd}.txt";
        foreach (var c in Path.GetInvalidFileNameChars())
            filename = filename.Replace(c, '_');
        var path = Path.Combine(folder, filename);
        File.WriteAllText(path, text, Encoding.UTF8);
        return path;
    }

    private static string GetSupplierName(Supplier supplier)
    {
        return string.IsNullOrWhiteSpace(supplier.Name) ? supplier.Company : supplier.Name;
    }
}
