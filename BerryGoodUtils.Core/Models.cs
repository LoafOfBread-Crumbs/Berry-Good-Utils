using System.Collections.ObjectModel;
using System.IO;

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
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal DefaultPrice { get; set; }
    public string PartNumber { get; set; } = string.Empty;
    public string ModelNumber { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
    public string AdditionalIdentifier { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string ReferenceImagePath { get; set; } = string.Empty;

    public string Manufacturer { get; set; } = string.Empty;
    public string EquipmentType { get; set; } = string.Empty;
    public string Voltage { get; set; } = string.Empty;
    public string Amps { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public string Phase { get; set; } = string.Empty;
    public string Horsepower { get; set; } = string.Empty;
    public string Kilowatts { get; set; } = string.Empty;
    public string IPRating { get; set; } = string.Empty;
    public string Refrigerant { get; set; } = string.Empty;
    public string RefrigerantCharge { get; set; } = string.Empty;
    public string MaxCellPressure { get; set; } = string.Empty;
    public string MaxPressure { get; set; } = string.Empty;
    public string MaxHead { get; set; } = string.Empty;
    public string TargetOutput { get; set; } = string.Empty;
    public string GrossWeight { get; set; } = string.Empty;
    public string ApprovalNumber { get; set; } = string.Empty;
    public string BuildDate { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string CountryOfManufacture { get; set; } = string.Empty;

    public List<string> IncludedEmailFields { get; set; } = [];
    public bool HasReferenceImage => !string.IsNullOrWhiteSpace(ReferenceImagePath) && File.Exists(ReferenceImagePath);
}

public class PartRequestItem
{
    public string PartOrService { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public bool IncludeReferenceImage { get; set; }
}

public class QuoteItem
{
    public string PartOrService { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public bool IncludeReferenceImage { get; set; }
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
