using System.IO;
using System.Text.Json;
using BerryGoodUtils.Models;

namespace BerryGoodUtils.Services;

public static class DataService
{
    private static readonly string DataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "BerryGoodUtils");

    private static readonly string DataFile = Path.Combine(DataFolder, "appdata.json");
    private static readonly string QuotesFolder = Path.Combine(DataFolder, "Quotes");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static AppData LoadAppData()
    {
        if (!Directory.Exists(DataFolder))
            Directory.CreateDirectory(DataFolder);

        if (!Directory.Exists(QuotesFolder))
            Directory.CreateDirectory(QuotesFolder);

        if (!File.Exists(DataFile))
            return new AppData();

        try
        {
            var json = File.ReadAllText(DataFile);
            return JsonSerializer.Deserialize<AppData>(json, JsonOptions) ?? new AppData();
        }
        catch
        {
            return new AppData();
        }
    }

    public static void SaveAppData(AppData data)
    {
        if (!Directory.Exists(DataFolder))
            Directory.CreateDirectory(DataFolder);

        var json = JsonSerializer.Serialize(data, JsonOptions);
        File.WriteAllText(DataFile, json);
    }

    public static string GetNextQuoteNumber(AppData data)
    {
        var number = $"Q-{data.NextQuoteNumber:D5}";
        data.NextQuoteNumber++;
        SaveAppData(data);
        return number;
    }

    public static string GetQuotesFolder()
    {
        if (!Directory.Exists(QuotesFolder))
            Directory.CreateDirectory(QuotesFolder);
        return QuotesFolder;
    }
}
