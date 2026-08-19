using System.Collections.ObjectModel;

namespace QuoteGenerator.Models;

public class AppData
{
    public CompanyInfo Company { get; set; } = new();
    public ObservableCollection<SavedPart> SavedParts { get; set; } = new();
    public int NextQuoteNumber { get; set; } = 1001;
}
