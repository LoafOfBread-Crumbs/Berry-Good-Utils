using System.Collections.ObjectModel;
using System.Text.Json;
using BerryGoodUtils.Core.Documents;
using BerryGoodUtils.Core.Email;
using BerryGoodUtils.Models;

namespace BerryGoodUtils.Core.Tests;

public class CoreTests
{
    [Fact]
    public void PartRequestHtmlEscapesUserContent()
    {
        var supplier = new Supplier { Name = "Parts <Co>", Email = "parts@example.com" };
        var items = new[] { new PartRequestItem { PartOrService = "Bolt & Nut", Quantity = 2 } };

        var html = PartRequestDocumentComposer.GenerateHtml(supplier, items, new CompanyInfo { CompanyName = "Berry & Co" });

        Assert.Contains("Parts &lt;Co&gt;", html);
        Assert.Contains("Bolt &amp; Nut", html);
        Assert.Contains("Berry &amp; Co", html);
    }

    [Fact]
    public void QuoteEmailUsesCustomerAndQuoteDetails()
    {
        var quote = new Quote
        {
            QuoteNumber = "Q-01001",
            CustomerName = "Josh",
            CustomerEmail = "josh@example.com",
            Items = new ObservableCollection<QuoteItem> { new() { PartOrService = "Work", Quantity = 2, UnitPrice = 10 } }
        };

        var message = EmailMessageFactory.ForQuote(quote, new CompanyInfo { CompanyName = "Berry Good" });

        Assert.Equal("josh@example.com", message.To);
        Assert.Contains("Q-01001", message.Subject);
        Assert.Contains("$20.00", message.HtmlContent);
        EmailMessageFactory.Validate(message);
    }

    [Fact]
    public void ValidationRejectsMissingRecipient()
    {
        var message = new EmailMessage { Subject = "Test" };

        Assert.Throws<FormatException>(() => EmailMessageFactory.Validate(message));
    }

    [Fact]
    public void ExistingAppDataJsonShapeDeserializes()
    {
        const string json = """
            {"Company":{"CompanyName":"Example"},"SavedParts":[],"Customers":[],"Suppliers":[],"NextQuoteNumber":1234}
            """;

        var data = JsonSerializer.Deserialize<AppData>(json);

        Assert.NotNull(data);
        Assert.Equal("Example", data.Company.CompanyName);
        Assert.Equal(1234, data.NextQuoteNumber);
    }
}
