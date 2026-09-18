using System.Net;
using System.Net.Mail;
using BerryGoodUtils.Models;

namespace BerryGoodUtils.Core.Email;

public sealed class EmailAttachment
{
    public string ContentId { get; set; } = Guid.NewGuid().ToString("N");
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";

    public byte[] ReadBytes() => File.Exists(FilePath) ? File.ReadAllBytes(FilePath) : [];
}

public sealed class EmailMessage
{
    public string To { get; set; } = string.Empty;
    public string Cc { get; set; } = string.Empty;
    public string Bcc { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Introduction { get; set; } = string.Empty;
    public string HtmlContent { get; set; } = string.Empty;
    public string TextContent { get; set; } = string.Empty;
    public List<EmailAttachment> Attachments { get; set; } = [];
}

public sealed record EmailAccountStatus(bool IsConfigured, bool IsSignedIn, string? EmailAddress, string? Message = null);

public interface IEmailSender
{
    Task<EmailAccountStatus> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<string> SignInAsync(CancellationToken cancellationToken = default);
    Task SignOutAsync(CancellationToken cancellationToken = default);
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}

public static class EmailMessageFactory
{
    public static EmailMessage ForPartRequest(Supplier supplier, IReadOnlyCollection<PartRequestItem> items, CompanyInfo company, IEnumerable<EmailAttachment>? attachments = null, IEnumerable<SavedPart>? savedParts = null)
    {
        var supplierName = string.IsNullOrWhiteSpace(supplier.Name) ? supplier.Company : supplier.Name;
        var attachmentList = attachments?.ToList() ?? [];
        return new EmailMessage
        {
            To = supplier.Email,
            Subject = $"Parts / services request from {company.CompanyName}".Trim(),
            Introduction = $"Hello {supplierName},\n\nPlease provide pricing and availability for the following items.",
            HtmlContent = Documents.PartRequestDocumentComposer.GenerateHtml(supplier, items, company, attachmentList, savedParts),
            TextContent = Documents.PartRequestDocumentComposer.GenerateText(supplier, items, company, attachmentList, savedParts),
            Attachments = attachmentList
        };
    }

    public static EmailMessage ForQuote(Quote quote, CompanyInfo company, IEnumerable<EmailAttachment>? attachments = null, IEnumerable<SavedPart>? savedParts = null)
    {
        var attachmentList = attachments?.ToList() ?? [];
        return new EmailMessage
        {
            To = quote.CustomerEmail,
            Subject = $"Quote {quote.QuoteNumber} from {company.CompanyName}".Trim(),
            Introduction = $"Hello {quote.CustomerName},\n\nPlease find your quote below.",
            HtmlContent = Documents.QuoteDocumentComposer.GenerateHtml(quote, company, attachmentList, savedParts),
            TextContent = $"{quote.QuoteNumber}\n{quote.CustomerName}\nTotal: {quote.Subtotal:C2}",
            Attachments = attachmentList
        };
    }

    public static string BuildHtmlBody(EmailMessage message)
    {
        var introduction = WebUtility.HtmlEncode(message.Introduction).Replace("\r\n", "<br/>").Replace("\n", "<br/>");
        var introductionBlock = $"<div style=\"font-family:Arial,sans-serif\">{introduction}</div><br/>";
        var bodyStart = message.HtmlContent.IndexOf("<body>", StringComparison.OrdinalIgnoreCase);
        var html = bodyStart >= 0
            ? message.HtmlContent.Insert(bodyStart + "<body>".Length, introductionBlock)
            : $"<html><body>{introductionBlock}{message.HtmlContent}</body></html>";

        foreach (var attachment in message.Attachments.Where(a => !string.IsNullOrWhiteSpace(a.ContentId)))
        {
            var cid = $"cid:{attachment.ContentId}";
            if (!html.Contains(cid, StringComparison.OrdinalIgnoreCase))
                continue;

            var dataUri = ToDataUri(attachment);
            if (dataUri != null)
                html = html.Replace(cid, dataUri, StringComparison.OrdinalIgnoreCase);
        }

        return html;
    }

    private static string? ToDataUri(EmailAttachment attachment)
    {
        try
        {
            var bytes = attachment.ReadBytes();
            if (bytes.Length == 0)
                return null;
            var base64 = Convert.ToBase64String(bytes);
            var mime = string.IsNullOrWhiteSpace(attachment.ContentType) ? "application/octet-stream" : attachment.ContentType;
            return $"data:{mime};base64,{base64}";
        }
        catch
        {
            return null;
        }
    }

    public static IReadOnlyList<string> ParseAddresses(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return [];

        var addresses = new List<string>();
        foreach (var entry in value.Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            addresses.Add(new MailAddress(entry).Address);
        return addresses;
    }

    public static void Validate(EmailMessage message)
    {
        if (ParseAddresses(message.To).Count == 0)
            throw new FormatException("At least one valid recipient email address is required.");
        ParseAddresses(message.Cc);
        ParseAddresses(message.Bcc);
        if (string.IsNullOrWhiteSpace(message.Subject))
            throw new FormatException("An email subject is required.");
    }
}
