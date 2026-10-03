using System.IO;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Drive.v3;
using Google.Apis.Gmail.v1;
using Google.Apis.Util.Store;
using BerryGoodUtils.Services.Email;

namespace BerryGoodUtils.Services.Auth;

/// <summary>
/// Shared Google OAuth credential provider for the desktop app.
/// Authorizes once for Gmail and Google Calendar scopes and exposes the credential to consumers.
/// </summary>
public sealed class GoogleAuthService
{
    private const string ApplicationName = "BerryGoodUtils";
    private readonly string _configurationPath;
    private readonly ProtectedTokenStore _tokenStore;
    private UserCredential? _credential;

    public GoogleAuthService()
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BerryGoodUtils");
        _configurationPath = Path.Combine(folder, "gmail-oauth-client.json");
        _tokenStore = new ProtectedTokenStore(Path.Combine(folder, "GoogleTokens"));
    }

    public bool IsConfigured => File.Exists(_configurationPath);
    public bool HasStoredTokens => _tokenStore.HasStoredTokens;
    public bool IsSignedIn => IsConfigured && HasStoredTokens;

    public async Task<UserCredential> GetCredentialAsync(CancellationToken cancellationToken = default)
    {
        if (_credential != null)
            return _credential;

        if (!IsConfigured)
            throw new InvalidOperationException($"Google OAuth is not configured. Add the downloaded Desktop OAuth JSON file at {_configurationPath}");

        await using var stream = File.OpenRead(_configurationPath);
        var secrets = GoogleClientSecrets.FromStream(stream).Secrets;
        _credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            secrets,
            [GmailService.Scope.GmailSend, GmailService.Scope.GmailMetadata, CalendarService.Scope.CalendarEvents,
                DriveService.Scope.DriveFile, DriveService.Scope.DriveReadonly],
            ApplicationName,
            cancellationToken,
            _tokenStore);
        return _credential;
    }

    public async Task RevokeAsync(CancellationToken cancellationToken = default)
    {
        if (_credential != null)
        {
            try
            {
                await _credential.RevokeTokenAsync(cancellationToken);
            }
            catch
            {
            }
        }
        await _tokenStore.ClearAsync();
        _credential = null;
    }
}
