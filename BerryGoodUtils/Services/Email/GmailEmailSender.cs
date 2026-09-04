using System.IO;
using BerryGoodUtils.Core.Email;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using MimeKit;

namespace BerryGoodUtils.Services.Email;

public sealed class GmailEmailSender : IEmailSender
{
    private const string UserId = "me";
    private readonly string _configurationPath;
    private readonly ProtectedTokenStore _tokenStore;
    private UserCredential? _credential;
    private string? _emailAddress;

    public GmailEmailSender()
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BerryGoodUtils");
        _configurationPath = Path.Combine(folder, "gmail-oauth-client.json");
        _tokenStore = new ProtectedTokenStore(Path.Combine(folder, "GmailTokens"));
    }

    public async Task<EmailAccountStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_configurationPath))
            return new(false, false, null, $"Gmail OAuth is not configured. Add the downloaded Desktop OAuth JSON file at {_configurationPath}");
        if (_credential == null && _tokenStore.HasStoredTokens)
        {
            try
            {
                await SignInAsync(cancellationToken);
            }
            catch
            {
                return new(true, false, null, "The saved Gmail session has expired. Please sign in again.");
            }
        }
        if (_credential == null)
            return new(true, false, null);
        if (string.IsNullOrWhiteSpace(_emailAddress))
            _emailAddress = await GetEmailAddressAsync(_credential, cancellationToken);
        return new(true, true, _emailAddress);
    }

    public async Task<string> SignInAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_configurationPath))
            throw new InvalidOperationException($"Gmail OAuth configuration was not found. Download Desktop OAuth credentials and save them as {_configurationPath}");

        await using var stream = File.OpenRead(_configurationPath);
        var secrets = GoogleClientSecrets.FromStream(stream).Secrets;
        _credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            secrets,
            [GmailService.Scope.GmailSend, GmailService.Scope.GmailMetadata],
            "BerryGoodUtils",
            cancellationToken,
            _tokenStore);
        _emailAddress = await GetEmailAddressAsync(_credential, cancellationToken);
        return _emailAddress;
    }

    public async Task SignOutAsync(CancellationToken cancellationToken = default)
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
        _emailAddress = null;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        EmailMessageFactory.Validate(message);
        if (_credential == null)
            await SignInAsync(cancellationToken);

        var mime = new MimeMessage();
        mime.From.Add(MailboxAddress.Parse(_emailAddress ?? await GetEmailAddressAsync(_credential!, cancellationToken)));
        AddAddresses(mime.To, message.To);
        AddAddresses(mime.Cc, message.Cc);
        AddAddresses(mime.Bcc, message.Bcc);
        mime.Subject = message.Subject;

        var builder = new BodyBuilder
        {
            HtmlBody = EmailMessageFactory.BuildHtmlBody(message),
            TextBody = $"{message.Introduction}\n\n{message.TextContent}"
        };
        mime.Body = builder.ToMessageBody();

        await using var memory = new MemoryStream();
        await mime.WriteToAsync(memory, cancellationToken);
        var raw = Convert.ToBase64String(memory.ToArray()).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        using var service = CreateService(_credential!);
        await service.Users.Messages.Send(new Message { Raw = raw }, UserId).ExecuteAsync(cancellationToken);
    }

    private async Task<string> GetEmailAddressAsync(UserCredential credential, CancellationToken cancellationToken)
    {
        using var service = CreateService(credential);
        var profile = await service.Users.GetProfile(UserId).ExecuteAsync(cancellationToken);
        return profile.EmailAddress;
    }

    private static GmailService CreateService(UserCredential credential)
    {
        return new GmailService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "Berry Good Utils"
        });
    }

    private static void AddAddresses(InternetAddressList target, string addresses)
    {
        foreach (var address in EmailMessageFactory.ParseAddresses(addresses))
            target.Add(MailboxAddress.Parse(address));
    }
}
