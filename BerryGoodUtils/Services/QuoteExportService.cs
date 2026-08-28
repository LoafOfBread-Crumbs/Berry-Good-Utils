using System.IO;
using System.Text;
using BerryGoodUtils.Models;

namespace BerryGoodUtils.Services;

public static class QuoteExportService
{
    public static string GenerateHtml(Quote quote, CompanyInfo company)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html><head><meta charset='utf-8'/>");
        sb.AppendLine("<style>");
        sb.AppendLine("body { font-family: 'Segoe UI', Arial, sans-serif; margin: 40px; color: #333; }");
        sb.AppendLine(".header { display: flex; justify-content: space-between; border-bottom: 3px solid #2563eb; padding-bottom: 20px; margin-bottom: 30px; }");
        sb.AppendLine(".company-name { font-size: 28px; font-weight: bold; color: #2563eb; }");
        sb.AppendLine(".company-details { font-size: 13px; color: #666; margin-top: 5px; }");
        sb.AppendLine(".quote-title { font-size: 32px; color: #2563eb; text-align: right; }");
        sb.AppendLine(".quote-meta { text-align: right; font-size: 14px; color: #666; }");
        sb.AppendLine(".section { margin-bottom: 25px; }");
        sb.AppendLine(".section-title { font-size: 16px; font-weight: bold; color: #2563eb; margin-bottom: 8px; border-bottom: 1px solid #ddd; padding-bottom: 4px; }");
        sb.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 10px; }");
        sb.AppendLine("th { background: #2563eb; color: white; padding: 10px 12px; text-align: left; font-size: 14px; }");
        sb.AppendLine("th:nth-child(3), th:nth-child(4) { text-align: right; }");
        sb.AppendLine("td { padding: 9px 12px; border-bottom: 1px solid #e5e7eb; font-size: 14px; }");
        sb.AppendLine("td:nth-child(3), td:nth-child(4) { text-align: right; }");
        sb.AppendLine("tr:nth-child(even) { background: #f8fafc; }");
        sb.AppendLine(".totals { text-align: right; margin-top: 15px; font-size: 18px; }");
        sb.AppendLine(".totals .label { color: #666; }");
        sb.AppendLine(".totals .amount { font-weight: bold; color: #2563eb; font-size: 22px; }");
        sb.AppendLine(".notes { background: #f8fafc; border-left: 4px solid #2563eb; padding: 12px 16px; margin-top: 25px; font-size: 14px; }");
        sb.AppendLine(".footer { margin-top: 40px; text-align: center; font-size: 12px; color: #999; border-top: 1px solid #ddd; padding-top: 15px; }");
        sb.AppendLine("@media print { body { margin: 20px; } }");
        sb.AppendLine("</style></head><body>");

        // Header
        sb.AppendLine("<div class='header'>");
        sb.AppendLine("<div>");
        sb.AppendLine($"<div class='company-name'>{Escape(company.CompanyName)}</div>");
        sb.AppendLine("<div class='company-details'>");
        if (!string.IsNullOrWhiteSpace(company.Address))
            sb.AppendLine($"{Escape(company.Address)}<br/>");
        if (!string.IsNullOrWhiteSpace(company.Phone))
            sb.AppendLine($"Phone: {Escape(company.Phone)}<br/>");
        if (!string.IsNullOrWhiteSpace(company.Email))
            sb.AppendLine($"Email: {Escape(company.Email)}");
        sb.AppendLine("</div></div>");
        sb.AppendLine("<div>");
        sb.AppendLine("<div class='quote-title'>QUOTE</div>");
        sb.AppendLine("<div class='quote-meta'>");
        sb.AppendLine($"Quote #: <strong>{Escape(quote.QuoteNumber)}</strong><br/>");
        sb.AppendLine($"Date: {quote.Date:MMMM dd, yyyy}");
        sb.AppendLine("</div></div></div>");

        // Customer info
        sb.AppendLine("<div class='section'>");
        sb.AppendLine("<div class='section-title'>Bill To</div>");
        sb.AppendLine($"<strong>{Escape(quote.CustomerName)}</strong><br/>");
        if (!string.IsNullOrWhiteSpace(quote.CustomerAddress))
            sb.AppendLine($"{Escape(quote.CustomerAddress)}<br/>");
        if (!string.IsNullOrWhiteSpace(quote.CustomerPhone))
            sb.AppendLine($"Phone: {Escape(quote.CustomerPhone)}<br/>");
        if (!string.IsNullOrWhiteSpace(quote.CustomerEmail))
            sb.AppendLine($"Email: {Escape(quote.CustomerEmail)}");
        sb.AppendLine("</div>");

        // Items table
        sb.AppendLine("<table>");
        sb.AppendLine("<tr><th>Part / Service</th><th>Description</th><th>Qty</th><th>Unit Price</th><th style='text-align:right'>Total</th></tr>");
        foreach (var item in quote.Items)
        {
            sb.AppendLine($"<tr><td>{Escape(item.PartOrService)}</td><td>{Escape(item.Description)}</td><td style='text-align:center'>{item.Quantity}</td><td>${item.UnitPrice:N2}</td><td style='text-align:right'>${item.Total:N2}</td></tr>");
        }
        sb.AppendLine("</table>");

        // Total
        sb.AppendLine("<div class='totals'>");
        sb.AppendLine($"<span class='label'>Total: </span><span class='amount'>${quote.Subtotal:N2}</span>");
        sb.AppendLine("</div>");

        // Notes
        if (!string.IsNullOrWhiteSpace(quote.Notes))
        {
            sb.AppendLine($"<div class='notes'><strong>Notes:</strong><br/>{Escape(quote.Notes).Replace("\n", "<br/>")}</div>");
        }

        sb.AppendLine("<div class='footer'>Thank you for your business!</div>");
        sb.AppendLine("</body></html>");

        return sb.ToString();
    }

    public static string SaveQuoteHtml(Quote quote, CompanyInfo company)
    {
        return SaveQuoteHtml(quote, company, DataService.GetQuotesFolder());
    }

    public static string SaveQuoteHtml(Quote quote, CompanyInfo company, Customer customer)
    {
        return SaveQuoteHtml(quote, company, CustomerService.GetQuotesFolder(customer));
    }

    public static string SaveQuoteHtml(Quote quote, CompanyInfo company, string folder)
    {
        var html = GenerateHtml(quote, company);
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);
        var filename = $"{quote.QuoteNumber}_{quote.CustomerName.Replace(" ", "_")}_{quote.Date:yyyyMMdd}.html";
        foreach (var c in Path.GetInvalidFileNameChars())
            filename = filename.Replace(c, '_');
        var path = Path.Combine(folder, filename);
        File.WriteAllText(path, html, Encoding.UTF8);
        return path;
    }

    private static string Escape(string text)
    {
        return System.Net.WebUtility.HtmlEncode(text ?? string.Empty);
    }
}
