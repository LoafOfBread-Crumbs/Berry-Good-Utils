using System.Text;
using BerryGoodUtils.Models;

namespace BerryGoodUtils.Core.Documents;

public static class PartRequestDocumentComposer
{
    public static string GenerateHtml(Supplier supplier, IEnumerable<PartRequestItem> items, CompanyInfo company)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html><head><meta charset='utf-8'/>");
        sb.AppendLine("<style>");
        sb.AppendLine("body { font-family: 'Segoe UI', Arial, sans-serif; margin: 40px; color: #333; }");
        sb.AppendLine(".header { border-bottom: 3px solid #1FA6C8; padding-bottom: 20px; margin-bottom: 30px; }");
        sb.AppendLine(".company-name { font-size: 24px; font-weight: bold; color: #1FA6C8; }");
        sb.AppendLine(".company-details { font-size: 13px; color: #666; margin-top: 5px; }");
        sb.AppendLine(".section-title { font-size: 16px; font-weight: bold; color: #1FA6C8; margin-bottom: 8px; border-bottom: 1px solid #ddd; padding-bottom: 4px; }");
        sb.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 10px; }");
        sb.AppendLine("th { background: #1FA6C8; color: white; padding: 10px 12px; text-align: left; font-size: 14px; }");
        sb.AppendLine("td { padding: 9px 12px; border-bottom: 1px solid #e5e7eb; font-size: 14px; }");
        sb.AppendLine("tr:nth-child(even) { background: #f8fafc; }");
        sb.AppendLine(".footer { margin-top: 40px; text-align: center; font-size: 12px; color: #999; border-top: 1px solid #ddd; padding-top: 15px; }");
        sb.AppendLine("</style></head><body>");
        sb.AppendLine("<div class='header'>");
        sb.AppendLine($"<div class='company-name'>{Escape(company.CompanyName)}</div>");
        sb.AppendLine("<div class='company-details'>");
        if (!string.IsNullOrWhiteSpace(company.Address)) sb.AppendLine($"{Escape(company.Address)}<br/>");
        if (!string.IsNullOrWhiteSpace(company.Phone)) sb.AppendLine($"Phone: {Escape(company.Phone)}<br/>");
        if (!string.IsNullOrWhiteSpace(company.Email)) sb.AppendLine($"Email: {Escape(company.Email)}");
        sb.AppendLine("</div></div>");
        sb.AppendLine("<div class='section'>");
        sb.AppendLine("<div class='section-title'>Supplier</div>");
        sb.AppendLine($"<strong>{Escape(GetSupplierName(supplier))}</strong><br/>");
        if (!string.IsNullOrWhiteSpace(supplier.Company)) sb.AppendLine($"{Escape(supplier.Company)}<br/>");
        if (!string.IsNullOrWhiteSpace(supplier.Address)) sb.AppendLine($"{Escape(supplier.Address)}<br/>");
        if (!string.IsNullOrWhiteSpace(supplier.Phone)) sb.AppendLine($"Phone: {Escape(supplier.Phone)}<br/>");
        if (!string.IsNullOrWhiteSpace(supplier.Email)) sb.AppendLine($"Email: {Escape(supplier.Email)}");
        sb.AppendLine("</div>");
        sb.AppendLine("<div class='section'>");
        sb.AppendLine("<div class='section-title'>Parts / Services Requested</div>");
        sb.AppendLine("<p>Please provide pricing and availability for the following items:</p>");
        sb.AppendLine("<table><tr><th>Part / Service</th><th>Qty Needed</th></tr>");
        foreach (var item in items) sb.AppendLine($"<tr><td>{Escape(item.PartOrService)}</td><td>{item.Quantity}</td></tr>");
        sb.AppendLine("</table>");
        sb.AppendLine("</div>");
        sb.AppendLine("<div class='footer'>Thank you for your time.</div>");
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    public static string GenerateText(Supplier supplier, IEnumerable<PartRequestItem> items, CompanyInfo company)
    {
        var sb = new StringBuilder();
        sb.AppendLine(company.CompanyName);
        if (!string.IsNullOrWhiteSpace(company.Address)) sb.AppendLine(company.Address);
        if (!string.IsNullOrWhiteSpace(company.Phone)) sb.AppendLine($"Phone: {company.Phone}");
        if (!string.IsNullOrWhiteSpace(company.Email)) sb.AppendLine($"Email: {company.Email}");
        sb.AppendLine();
        sb.AppendLine($"To: {GetSupplierName(supplier)}");
        if (!string.IsNullOrWhiteSpace(supplier.Company)) sb.AppendLine($"Company: {supplier.Company}");
        if (!string.IsNullOrWhiteSpace(supplier.Address)) sb.AppendLine(supplier.Address);
        if (!string.IsNullOrWhiteSpace(supplier.Phone)) sb.AppendLine($"Phone: {supplier.Phone}");
        if (!string.IsNullOrWhiteSpace(supplier.Email)) sb.AppendLine($"Email: {supplier.Email}");
        sb.AppendLine();
        sb.AppendLine("Please provide pricing and availability for the following items:");
        sb.AppendLine();
        sb.AppendLine($"{"Part / Service",-40} {"Qty Needed",10}");
        sb.AppendLine(new string('-', 52));
        foreach (var item in items) sb.AppendLine($"{item.PartOrService,-40} {item.Quantity,10}");
        sb.AppendLine();
        sb.AppendLine("Thank you for your time.");
        return sb.ToString();
    }

    private static string GetSupplierName(Supplier supplier) => string.IsNullOrWhiteSpace(supplier.Name) ? supplier.Company : supplier.Name;
    private static string Escape(string text) => System.Net.WebUtility.HtmlEncode(text ?? string.Empty);
}

public static class QuoteDocumentComposer
{
    public static string GenerateHtml(Quote quote, CompanyInfo company)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html><head><meta charset='utf-8'/>");
        sb.AppendLine("<style>");
        sb.AppendLine("body { font-family: 'Segoe UI', Arial, sans-serif; margin: 40px; color: #333; }");
        sb.AppendLine(".header { display: flex; justify-content: space-between; border-bottom: 3px solid #1FA6C8; padding-bottom: 20px; margin-bottom: 30px; }");
        sb.AppendLine(".company-name { font-size: 28px; font-weight: bold; color: #1FA6C8; }");
        sb.AppendLine(".company-details { font-size: 13px; color: #666; margin-top: 5px; }");
        sb.AppendLine(".quote-title { font-size: 32px; color: #1FA6C8; text-align: right; }");
        sb.AppendLine(".quote-meta { text-align: right; font-size: 14px; color: #666; }");
        sb.AppendLine(".section { margin-bottom: 25px; }");
        sb.AppendLine(".section-title { font-size: 16px; font-weight: bold; color: #1FA6C8; margin-bottom: 8px; border-bottom: 1px solid #ddd; padding-bottom: 4px; }");
        sb.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 10px; }");
        sb.AppendLine("th { background: #1FA6C8; color: white; padding: 10px 12px; text-align: left; font-size: 14px; }");
        sb.AppendLine("th:nth-child(3), th:nth-child(4) { text-align: right; }");
        sb.AppendLine("td { padding: 9px 12px; border-bottom: 1px solid #e5e7eb; font-size: 14px; }");
        sb.AppendLine("td:nth-child(3), td:nth-child(4) { text-align: right; }");
        sb.AppendLine("tr:nth-child(even) { background: #f8fafc; }");
        sb.AppendLine(".totals { text-align: right; margin-top: 15px; font-size: 18px; }");
        sb.AppendLine(".totals .label { color: #666; }");
        sb.AppendLine(".totals .amount { font-weight: bold; color: #1FA6C8; font-size: 22px; }");
        sb.AppendLine(".notes { background: #f8fafc; border-left: 4px solid #1FA6C8; padding: 12px 16px; margin-top: 25px; font-size: 14px; }");
        sb.AppendLine(".footer { margin-top: 40px; text-align: center; font-size: 12px; color: #999; border-top: 1px solid #ddd; padding-top: 15px; }");
        sb.AppendLine("@media print { body { margin: 20px; } }");
        sb.AppendLine("</style></head><body>");
        sb.AppendLine("<div class='header'><div>");
        sb.AppendLine($"<div class='company-name'>{Escape(company.CompanyName)}</div>");
        sb.AppendLine("<div class='company-details'>");
        if (!string.IsNullOrWhiteSpace(company.Address)) sb.AppendLine($"{Escape(company.Address)}<br/>");
        if (!string.IsNullOrWhiteSpace(company.Phone)) sb.AppendLine($"Phone: {Escape(company.Phone)}<br/>");
        if (!string.IsNullOrWhiteSpace(company.Email)) sb.AppendLine($"Email: {Escape(company.Email)}");
        sb.AppendLine("</div></div><div>");
        sb.AppendLine("<div class='quote-title'>QUOTE</div>");
        sb.AppendLine("<div class='quote-meta'>");
        sb.AppendLine($"Quote #: <strong>{Escape(quote.QuoteNumber)}</strong><br/>");
        sb.AppendLine($"Date: {quote.Date:MMMM dd, yyyy}");
        sb.AppendLine("</div></div></div>");
        sb.AppendLine("<div class='section'><div class='section-title'>Bill To</div>");
        sb.AppendLine($"<strong>{Escape(quote.CustomerName)}</strong><br/>");
        if (!string.IsNullOrWhiteSpace(quote.CustomerAddress)) sb.AppendLine($"{Escape(quote.CustomerAddress)}<br/>");
        if (!string.IsNullOrWhiteSpace(quote.CustomerPhone)) sb.AppendLine($"Phone: {Escape(quote.CustomerPhone)}<br/>");
        if (!string.IsNullOrWhiteSpace(quote.CustomerEmail)) sb.AppendLine($"Email: {Escape(quote.CustomerEmail)}");
        sb.AppendLine("</div><table>");
        sb.AppendLine("<tr><th>Part / Service</th><th>Description</th><th>Qty</th><th>Unit Price</th><th style='text-align:right'>Total</th></tr>");
        foreach (var item in quote.Items) sb.AppendLine($"<tr><td>{Escape(item.PartOrService)}</td><td>{Escape(item.Description)}</td><td style='text-align:center'>{item.Quantity}</td><td>${item.UnitPrice:N2}</td><td style='text-align:right'>${item.Total:N2}</td></tr>");
        sb.AppendLine("</table>");
        sb.AppendLine($"<div class='totals'><span class='label'>Total: </span><span class='amount'>${quote.Subtotal:N2}</span></div>");
        if (!string.IsNullOrWhiteSpace(quote.Notes)) sb.AppendLine($"<div class='notes'><strong>Notes:</strong><br/>{Escape(quote.Notes).Replace("\n", "<br/>")}</div>");
        sb.AppendLine("<div class='footer'>Thank you for your business!</div>");
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private static string Escape(string text) => System.Net.WebUtility.HtmlEncode(text ?? string.Empty);
}
