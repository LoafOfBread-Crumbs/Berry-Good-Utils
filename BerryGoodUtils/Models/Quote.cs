using System.Collections.ObjectModel;

namespace BerryGoodUtils.Models;

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
