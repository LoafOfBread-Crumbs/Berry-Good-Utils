using System.IO;
using System.Text;
using BerryGoodUtils.Core.Documents;
using BerryGoodUtils.Models;

namespace BerryGoodUtils.Services;

public static class QuoteExportService
{
    public static string GenerateHtml(Quote quote, CompanyInfo company)
    {
        return QuoteDocumentComposer.GenerateHtml(quote, company);
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
}
