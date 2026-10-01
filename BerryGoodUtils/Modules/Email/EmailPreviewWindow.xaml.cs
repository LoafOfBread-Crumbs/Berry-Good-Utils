using System.Windows;
using BerryGoodUtils.Core.Email;
using BerryGoodUtils.Models;
using BerryGoodUtils.Modules.Common;

namespace BerryGoodUtils.Modules.Email;

public partial class EmailPreviewWindow : Window
{
    private readonly EmailMessage _message;
    private readonly IEmailSender _emailSender;
    private readonly AppData _appData;
    private readonly Func<EmailMessage> _refreshMessage;

    public EmailPreviewWindow(EmailMessage message, IEmailSender emailSender, AppData appData, Func<EmailMessage> refreshMessage)
    {
        InitializeComponent();
        _message = message;
        _emailSender = emailSender;
        _appData = appData;
        _refreshMessage = refreshMessage;
        txtTo.Text = message.To;
        txtCc.Text = message.Cc;
        txtBcc.Text = message.Bcc;
        txtSubject.Text = message.Subject;
        txtIntroduction.Text = message.Introduction;
        Loaded += EmailPreviewWindow_Loaded;
    }

    private async void EmailPreviewWindow_Loaded(object sender, RoutedEventArgs e)
    {
        MaxHeight = Math.Max(MinHeight, SystemParameters.WorkArea.Height - 40);
        if (Height > MaxHeight)
            Height = MaxHeight;
        RefreshAttachmentLabel();
        await UpdatePreviewAsync();
        await RefreshAccountStatusAsync();
    }

    private void RefreshAttachmentLabel()
    {
        if (_message.Attachments.Count == 0)
        {
            txtAttachments.Visibility = Visibility.Collapsed;
            return;
        }

        txtAttachments.Text = _message.Attachments.Count == 1
            ? $"Attachment: {_message.Attachments[0].FileName}"
            : $"Attachments: {_message.Attachments.Count} files";
        txtAttachments.Visibility = Visibility.Visible;
    }

    private async Task RefreshAccountStatusAsync()
    {
        try
        {
            var status = await _emailSender.GetStatusAsync();
            txtAccount.Text = status.IsSignedIn
                ? $"Gmail: {status.EmailAddress}"
                : status.IsConfigured ? "Gmail: not signed in" : status.Message;
        }
        catch (Exception ex)
        {
            txtAccount.Text = $"Gmail: {ex.Message}";
        }
    }

    private async void CompanyDetails_Click(object sender, RoutedEventArgs e)
    {
        CopyFieldsToMessage();
        var settingsWindow = new CompanySettingsWindow(_appData) { Owner = this };
        if (settingsWindow.ShowDialog() != true)
            return;

        var refreshed = _refreshMessage();
        _message.Subject = refreshed.Subject;
        _message.HtmlContent = refreshed.HtmlContent;
        _message.TextContent = refreshed.TextContent;
        txtSubject.Text = _message.Subject;
        await UpdatePreviewAsync();
    }

    private async void Send_Click(object sender, RoutedEventArgs e)
    {
        CopyFieldsToMessage();
        try
        {
            EmailMessageFactory.Validate(_message);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Check Email", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var confirmation = MessageBox.Show($"Send this email to {_message.To}?", "Confirm Send", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirmation != MessageBoxResult.Yes)
            return;

        SetBusy(true);
        try
        {
            await _emailSender.SendAsync(_message);
            MessageBox.Show("Email sent successfully.", "Email Sent", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"The email was not sent.\n\n{ex.Message}", "Email Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void Introduction_Changed(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (IsLoaded)
            await UpdatePreviewAsync();
    }

    private async Task UpdatePreviewAsync()
    {
        if (browserPreview == null)
            return;
        _message.Introduction = txtIntroduction?.Text ?? _message.Introduction;
        await browserPreview.EnsureCoreWebView2Async();
        browserPreview.NavigateToString(EmailMessageFactory.BuildHtmlBody(_message));
    }

    private void CopyFieldsToMessage()
    {
        _message.To = txtTo.Text.Trim();
        _message.Cc = txtCc.Text.Trim();
        _message.Bcc = txtBcc.Text.Trim();
        _message.Subject = txtSubject.Text.Trim();
        _message.Introduction = txtIntroduction.Text.Trim();
    }

    private void SetBusy(bool busy)
    {
        btnSend.IsEnabled = !busy;
        btnCompanyDetails.IsEnabled = !busy;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
