using System.Collections.ObjectModel;

namespace BerryGoodUtils.Models;

public class AppData
{
    public CompanyInfo Company { get; set; } = new();
    public ObservableCollection<SavedPart> SavedParts { get; set; } = new();
    public ObservableCollection<Customer> Customers { get; set; } = new();
    public ObservableCollection<Supplier> Suppliers { get; set; } = new();
    public int NextQuoteNumber { get; set; } = 1001;
}

public class CompanyInfo
{
    public string CompanyName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class Customer
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class Supplier
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class SavedPart
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal DefaultPrice { get; set; }
}

public class PartRequestItem
{
    public string PartOrService { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
}

public class QuoteItem
{
    public string PartOrService { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal Total => Quantity * UnitPrice;
}

public class Quote
{
    public string QuoteNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Now;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string CustomerAddress { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public ObservableCollection<QuoteItem> Items { get; set; } = new();
    public decimal Subtotal => Items.Sum(i => i.Total);
}
