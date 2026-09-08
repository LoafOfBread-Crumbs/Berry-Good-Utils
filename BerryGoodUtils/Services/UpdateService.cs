using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;

namespace BerryGoodUtils.Services;

public static class UpdateService
{
    private const string RepositoryOwner = "LoafOfBread-Crumbs";
    private const string RepositoryName = "Berry-Good-Utils";
    private const string AssetName = "BerryGoodUtils.exe";

    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    static UpdateService()
    {
        HttpClient.DefaultRequestHeaders.Add("User-Agent", "BerryGoodUtils-UpdateChecker");
        HttpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
    }

    public static Version CurrentVersion =>
        Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0);

    public static async Task<UpdateCheckResult> CheckForUpdateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var release = await HttpClient.GetFromJsonAsync<GitHubRelease>(
                $"https://api.github.com/repos/{RepositoryOwner}/{RepositoryName}/releases/latest",
                cancellationToken);

            if (release == null)
                return new(false, CurrentVersion, CurrentVersion, "", "", "", "No release information was returned.");

            var latestVersion = ParseVersion(release.TagName);
            var asset = release.Assets
                .FirstOrDefault(a => a.Name.Equals(AssetName, StringComparison.OrdinalIgnoreCase));
            var downloadUrl = asset?.BrowserDownloadUrl ?? string.Empty;

            return new UpdateCheckResult(
                latestVersion > CurrentVersion,
                CurrentVersion,
                latestVersion,
                release.Name,
                release.Body,
                downloadUrl);
        }
        catch (HttpRequestException ex)
        {
            return new(false, CurrentVersion, CurrentVersion, "", "", "", $"Could not reach GitHub: {ex.Message}");
        }
        catch (Exception ex)
        {
            return new(false, CurrentVersion, CurrentVersion, "", "", "", $"Update check failed: {ex.Message}");
        }
    }

    public static async Task<string> DownloadUpdateAsync(
        string downloadUrl,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), "BerryGoodUtils_Update");
        if (Directory.Exists(tempFolder))
            Directory.Delete(tempFolder, true);
        Directory.CreateDirectory(tempFolder);

        var tempPath = Path.Combine(tempFolder, AssetName);
        using var response = await HttpClient.GetAsync(
            downloadUrl,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1L;
        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var fileStream = new FileStream(
            tempPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None);

        var buffer = new byte[8192];
        long totalRead = 0;
        int read;
        while ((read = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            totalRead += read;
            if (totalBytes > 0)
                progress?.Report(totalRead / (double)totalBytes);
        }

        return tempPath;
    }

    public static void ApplyUpdate(string downloadedExePath)
    {
        var currentExePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(currentExePath))
            throw new InvalidOperationException("Could not determine the current executable path.");

        if (currentExePath.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Updates can only be applied to a published BerryGoodUtils.exe, not when running under dotnet run.");

        var tempFolder = Path.GetDirectoryName(downloadedExePath)!;
        var scriptPath = Path.Combine(tempFolder, "apply-update.ps1");

        var script = new StringBuilder();
        script.AppendLine("$ErrorActionPreference = 'Stop'");
        script.AppendLine("Start-Sleep -Seconds 2");
        script.AppendLine($"Copy-Item -Path '{EscapePowerShellPath(downloadedExePath)}' -Destination '{EscapePowerShellPath(currentExePath)}' -Force");
        script.AppendLine($"Start-Process -FilePath '{EscapePowerShellPath(currentExePath)}'");
        script.AppendLine($"Remove-Item -Path '{EscapePowerShellPath(tempFolder)}' -Recurse -Force");

        File.WriteAllText(scriptPath, script.ToString());

        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-ExecutionPolicy Bypass -WindowStyle Hidden -File \"{scriptPath}\"",
            CreateNoWindow = true,
            UseShellExecute = false
        };

        Process.Start(startInfo);
    }

    private static Version ParseVersion(string tag)
    {
        var cleaned = tag.Trim().TrimStart('v', 'V');
        return Version.TryParse(cleaned, out var version) ? version : new Version(1, 0, 0);
    }

    private static string EscapePowerShellPath(string path) =>
        path.Replace("'", "''");
}
