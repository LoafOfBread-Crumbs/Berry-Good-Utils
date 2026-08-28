using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using BerryGoodUtils.Models;

namespace BerryGoodUtils.Services;

public static class PartRequestExportService
{
    public static string GenerateHtml(Supplier supplier, ObservableCollection<PartRequestItem> items, CompanyInfo company)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html><head><meta charset='utf-8'/>");
        sb.AppendLine("<style>");
        sb.AppendLine("body { font-family: 'Segoe UI', Arial, sans-serif; margin: 40px; color: #333; }");
        sb.AppendLine(".header { border-bottom: 3px solid #2563eb; padding-bottom: 20px; margin-bottom: 30px; }");
        sb.AppendLine(".company-name { font-size: 24px; font-weight: bold; color: #2563eb; }");
        sb.AppendLine(".company-details { font-size: 13px; color: #666; margin-top: 5px; }");
        sb.AppendLine(".section-title { font-size: 16px; font-weight: bold; color: #2563eb; margin-bottom: 8px; border-bottom: 1px solid #ddd; padding-bottom: 4px; }");
        sb.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 10px; }");
        sb.AppendLine("th { background: #2563eb; color: white; padding: 10px 12px; text-align: left; font-size: 14px; }");
        sb.AppendLine("td { padding: 9px 12px; border-bottom: 1px solid #e5e7eb; font-size: 14px; }");
        sb.AppendLine("tr:nth-child(even) { background: #f8fafc; }");
        sb.AppendLine(".footer { margin-top: 40px; text-align: center; font-size: 12px; color: #999; border-top: 1px solid #ddd; padding-top: 15px; }");
        sb.AppendLine("</style></head><body>");

        sb.AppendLine("<div class='header'>");
        sb.AppendLine($"<div class='company-name'>{Escape(company.CompanyName)}</div>");
        sb.AppendLine("<div class='company-details'>");
        if (!string.IsNullOrWhiteSpace(company.Address))
            sb.AppendLine($"{Escape(company.Address)}<br/>");
        if (!string.IsNullOrWhiteSpace(company.Phone))
            sb.AppendLine($"Phone: {Escape(company.Phone)}<br/>");
        if (!string.IsNullOrWhiteSpace(company.Email))
            sb.AppendLine($"Email: {Escape(company.Email)}");
        sb.AppendLine("</div></div>");

        sb.AppendLine("<div class='section'>");
        sb.AppendLine("<div class='section-title'>Supplier</div>");
        sb.AppendLine($"<strong>{Escape(GetSupplierName(supplier))}</strong><br/>");
        if (!string.IsNullOrWhiteSpace(supplier.Company))
            sb.AppendLine($"{Escape(supplier.Company)}<br/>");
        if (!string.IsNullOrWhiteSpace(supplier.Address))
            sb.AppendLine($"{Escape(supplier.Address)}<br/>");
        if (!string.IsNullOrWhiteSpace(supplier.Phone))
            sb.AppendLine($"Phone: {Escape(supplier.Phone)}<br/>");
        if (!string.IsNullOrWhiteSpace(supplier.Email))
            sb.AppendLine($"Email: {Escape(supplier.Email)}");
        sb.AppendLine("</div>");

        sb.AppendLine("<div class='section'>");
        sb.AppendLine("<div class='section-title'>Parts / Services Requested</div>");
        sb.AppendLine("<p>Please provide pricing and availability for the following items:</p>");
        sb.AppendLine("<table><tr><th>Part / Service</th><th>Qty Needed</th></tr>");
        foreach (var item in items)
        {
            sb.AppendLine($"<tr><td>{Escape(item.PartOrService)}</td><td>{item.Quantity}</td></tr>");
        }
        sb.AppendLine("</table>");
        sb.AppendLine("</div>");

        sb.AppendLine("<div class='footer'>Thank you for your time.</div>");
        sb.AppendLine("</body></html>");

        return sb.ToString();
    }

    public static string GenerateText(Supplier supplier, ObservableCollection<PartRequestItem> items, CompanyInfo company)
    {
        var sb = new StringBuilder();
        sb.AppendLine(company.CompanyName);
        if (!string.IsNullOrWhiteSpace(company.Address))
            sb.AppendLine(company.Address);
        if (!string.IsNullOrWhiteSpace(company.Phone))
            sb.AppendLine($"Phone: {company.Phone}");
        if (!string.IsNullOrWhiteSpace(company.Email))
            sb.AppendLine($"Email: {company.Email}");
        sb.AppendLine();

        sb.AppendLine($"To: {GetSupplierName(supplier)}");
        if (!string.IsNullOrWhiteSpace(supplier.Company))
            sb.AppendLine($"Company: {supplier.Company}");
        if (!string.IsNullOrWhiteSpace(supplier.Address))
            sb.AppendLine(supplier.Address);
        if (!string.IsNullOrWhiteSpace(supplier.Phone))
            sb.AppendLine($"Phone: {supplier.Phone}");
        if (!string.IsNullOrWhiteSpace(supplier.Email))
            sb.AppendLine($"Email: {supplier.Email}");
        sb.AppendLine();

        sb.AppendLine("Please provide pricing and availability for the following items:");
        sb.AppendLine();

        sb.AppendLine($"{"Part / Service",-40} {"Qty Needed",10}");
        sb.AppendLine(new string('-', 52));
        foreach (var item in items)
        {
            sb.AppendLine($"{item.PartOrService,-40} {item.Quantity,10}");
        }
        sb.AppendLine();
        sb.AppendLine("Thank you for your time.");

        return sb.ToString();
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

    private static string Escape(string text)
    {
        return System.Net.WebUtility.HtmlEncode(text ?? string.Empty);
    }
}
