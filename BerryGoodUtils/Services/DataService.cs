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
    private static readonly string PartImagesFolder = Path.Combine(DataFolder, "PartImages");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static AppData LoadAppData()
    {
        if (!Directory.Exists(DataFolder))
            Directory.CreateDirectory(DataFolder);

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

    public static string GetPartImagesFolder()
    {
        if (!Directory.Exists(PartImagesFolder))
            Directory.CreateDirectory(PartImagesFolder);
        return PartImagesFolder;
    }
}
