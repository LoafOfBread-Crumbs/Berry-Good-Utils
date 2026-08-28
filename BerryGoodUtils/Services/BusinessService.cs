using System;
using System.IO;

namespace BerryGoodUtils.Services;

public static class BusinessService
{
    private static readonly string RootFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "BerryGoodUtils",
        "Business");

    public static string GetRootFolder()
    {
        if (!Directory.Exists(RootFolder))
            Directory.CreateDirectory(RootFolder);
        return RootFolder;
    }

    public static string GetQuotesFolder()
    {
        var folder = Path.Combine(GetRootFolder(), "Quotes");
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);
        return folder;
    }

    public static string GetPartRequestsFolder()
    {
        var folder = Path.Combine(GetRootFolder(), "PartRequests");
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);
        return folder;
    }
}
