using System.IO;
using BerryGoodUtils.Core.Email;
using BerryGoodUtils.Services.Auth;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using MimeKit;

namespace BerryGoodUtils.Services.Email;

public sealed class GmailEmailSender : IEmailSender
{
    private const string UserId = "me";
    private readonly GoogleAuthService _authService;
    private string? _emailAddress;

    public GmailEmailSender(GoogleAuthService authService)
    {
        _authService = authService;
    }

    public Task<EmailAccountStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        if (!_authService.IsConfigured)
            return Task.FromResult(new EmailAccountStatus(false, false, null, "Gmail OAuth is not configured. Add the downloaded Desktop OAuth JSON file in the app data folder."));

        if (!_authService.IsSignedIn)
            return Task.FromResult(new EmailAccountStatus(true, false, null, "Not signed in. Sign in from the main dashboard."));

        return Task.FromResult(new EmailAccountStatus(true, true, _emailAddress, null));
    }

    public async Task<string> SignInAsync(CancellationToken cancellationToken = default)
    {
        var credential = await _authService.GetCredentialAsync(cancellationToken);
        _emailAddress = await GetEmailAddressAsync(credential, cancellationToken);
        return _emailAddress;
    }

    public Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        _emailAddress = null;
        return _authService.RevokeAsync(cancellationToken);
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        EmailMessageFactory.Validate(message);
        if (!_authService.HasStoredTokens)
            throw new InvalidOperationException("Please sign in to Google from the main dashboard before sending email.");
        var credential = await _authService.GetCredentialAsync(cancellationToken);

        var mime = new MimeMessage();
        mime.From.Add(MailboxAddress.Parse(_emailAddress ?? await GetEmailAddressAsync(credential, cancellationToken)));
        AddAddresses(mime.To, message.To);
        AddAddresses(mime.Cc, message.Cc);
        AddAddresses(mime.Bcc, message.Bcc);
        mime.Subject = message.Subject;

        var builder = new BodyBuilder
        {
            HtmlBody = EmailMessageFactory.BuildHtmlBody(message),
            TextBody = $"{message.Introduction}\n\n{message.TextContent}"
        };

        foreach (var attachment in message.Attachments)
        {
            if (!File.Exists(attachment.FilePath))
                continue;

            var bytes = File.ReadAllBytes(attachment.FilePath);
            var contentType = ContentType.Parse(attachment.ContentType);
            var part = new MimePart(contentType)
            {
                Content = new MimeContent(new MemoryStream(bytes)),
                ContentTransferEncoding = ContentEncoding.Base64,
                ContentDisposition = new ContentDisposition(ContentDisposition.Inline),
                ContentId = attachment.ContentId,
                FileName = attachment.FileName
            };
            builder.LinkedResources.Add(part);
        }

        mime.Body = builder.ToMessageBody();

        await using var memory = new MemoryStream();
        await mime.WriteToAsync(memory, cancellationToken);
        var raw = Convert.ToBase64String(memory.ToArray()).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        using var service = CreateService(credential);
        await service.Users.Messages.Send(new Message { Raw = raw }, UserId).ExecuteAsync(cancellationToken);
    }

    private async Task<string> GetEmailAddressAsync(Google.Apis.Auth.OAuth2.UserCredential credential, CancellationToken cancellationToken)
    {
        using var service = CreateService(credential);
        var profile = await service.Users.GetProfile(UserId).ExecuteAsync(cancellationToken);
        return profile.EmailAddress;
    }

    private static GmailService CreateService(Google.Apis.Auth.OAuth2.UserCredential credential)
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
