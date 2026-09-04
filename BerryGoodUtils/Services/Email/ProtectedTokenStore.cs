using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Google.Apis.Util.Store;

namespace BerryGoodUtils.Services.Email;

public sealed class ProtectedTokenStore : IDataStore
{
    private readonly string _folder;
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("BerryGoodUtils.GmailOAuth");

    public ProtectedTokenStore(string folder)
    {
        _folder = folder;
        Directory.CreateDirectory(_folder);
    }

    public bool HasStoredTokens => Directory.EnumerateFiles(_folder, "*.token").Any();

    public Task StoreAsync<T>(string key, T value)
    {
        var json = JsonSerializer.Serialize(value);
        var encrypted = ProtectedData.Protect(Encoding.UTF8.GetBytes(json), Entropy, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(GetPath(key), encrypted);
        return Task.CompletedTask;
    }

    public Task DeleteAsync<T>(string key)
    {
        var path = GetPath(key);
        if (File.Exists(path))
            File.Delete(path);
        return Task.CompletedTask;
    }

    public Task<T?> GetAsync<T>(string key)
    {
        var path = GetPath(key);
        if (!File.Exists(path))
            return Task.FromResult(default(T));
        var decrypted = ProtectedData.Unprotect(File.ReadAllBytes(path), Entropy, DataProtectionScope.CurrentUser);
        return Task.FromResult(JsonSerializer.Deserialize<T>(decrypted));
    }

    public Task ClearAsync()
    {
        if (Directory.Exists(_folder))
        {
            foreach (var file in Directory.EnumerateFiles(_folder, "*.token"))
                File.Delete(file);
        }
        return Task.CompletedTask;
    }

    private string GetPath(string key)
    {
        var safeKey = string.Concat(key.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
        return Path.Combine(_folder, $"{safeKey}.token");
    }
}
