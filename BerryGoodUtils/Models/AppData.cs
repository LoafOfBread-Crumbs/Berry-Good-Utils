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
