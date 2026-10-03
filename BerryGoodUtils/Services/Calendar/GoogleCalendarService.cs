using System.IO;
using BerryGoodUtils.Core.Scheduling;
using BerryGoodUtils.Models;
using BerryGoodUtils.Services.Auth;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Download;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using DriveFile = Google.Apis.Drive.v3.Data.File;

namespace BerryGoodUtils.Services.Calendar;

public sealed class GoogleCalendarService
{
    private readonly GoogleAuthService _authService;
    private const string ApplicationName = "Berry Good Utils";

    public GoogleCalendarService(GoogleAuthService authService)
    {
        _authService = authService;
    }

    public Task<GoogleCalendarStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        if (!_authService.IsConfigured)
            return Task.FromResult(new GoogleCalendarStatus(false, false, "Google OAuth is not configured. Add gmail-oauth-client.json to the app data folder."));
        if (!_authService.IsSignedIn)
            return Task.FromResult(new GoogleCalendarStatus(true, false, "Not signed in. Sign in from the main dashboard."));
        return Task.FromResult(new GoogleCalendarStatus(true, true, "Signed in to Google Calendar."));
    }

    public async Task<SyncResult> SyncScheduleAsync(CustomerSchedule schedule, Customer customer, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_authService.HasStoredTokens)
                return new(false, "Please sign in to Google from the main dashboard before syncing schedules.");

            var credential = await _authService.GetCredentialAsync(cancellationToken);
            using var service = CreateService(credential);
            using var driveService = CreateDriveService(credential);
            var imported = await ImportCommentsAsync(service, driveService, schedule, customer, cancellationToken);

            if (schedule.RecurrenceType == ScheduleRecurrenceType.SpecificDates)
                await SyncSpecificDatesAsync(service, schedule, customer, cancellationToken);
            else
                await SyncSingleOrRecurringAsync(service, schedule, customer, cancellationToken);

            var events = await GetOccurrenceEventsAsync(service, schedule, cancellationToken);
            var uploaded = await PushVisitDataAsync(service, driveService, schedule, events, cancellationToken);

            schedule.IsSynced = true;
            schedule.LastSyncedAt = DateTime.Now;
            schedule.SyncError = string.Empty;
            return new(true, $"Schedule synced. Imported {imported} and uploaded {uploaded} post-visit comment/photo item(s).", imported, uploaded);
        }
        catch (Exception ex)
        {
            schedule.IsSynced = false;
            schedule.SyncError = ex.Message;
            return new(false, $"Sync failed: {ex.Message}");
        }
    }

    public async Task<SyncResult> ImportScheduleCommentsAsync(CustomerSchedule schedule, Customer customer, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_authService.HasStoredTokens)
                return new(false, "Not signed in to Google Calendar.");
            if (string.IsNullOrWhiteSpace(schedule.GoogleCalendarEventId) && schedule.GoogleCalendarEventIds.Count == 0)
                return new(true, "Schedule has not been synced yet.");

            var credential = await _authService.GetCredentialAsync(cancellationToken);
            using var service = CreateService(credential);
            using var driveService = CreateDriveService(credential);
            var imported = await ImportCommentsAsync(service, driveService, schedule, customer, cancellationToken);
            schedule.LastSyncedAt = DateTime.Now;
            schedule.SyncError = string.Empty;
            return new(true, $"Imported {imported} post-visit comment/photo item(s).", imported);
        }
        catch (Exception ex)
        {
            schedule.SyncError = ex.Message;
            return new(false, $"Import failed: {ex.Message}");
        }
    }

    public async Task<SyncResult> DeleteScheduleAsync(CustomerSchedule schedule, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_authService.HasStoredTokens)
                return new(false, "Please sign in to Google from the main dashboard before deleting synced schedules.");
            var credential = await _authService.GetCredentialAsync(cancellationToken);
            using var service = CreateService(credential);
            var ids = new List<string>();
            if (!string.IsNullOrWhiteSpace(schedule.GoogleCalendarEventId))
                ids.Add(schedule.GoogleCalendarEventId);
            ids.AddRange(schedule.GoogleCalendarEventIds.Where(id => !string.IsNullOrWhiteSpace(id)));

            foreach (var id in ids.Distinct())
            {
                try
                {
                    await service.Events.Delete("primary", id).ExecuteAsync(cancellationToken);
                }
                catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
                {
                }
            }

            schedule.GoogleCalendarEventId = string.Empty;
            schedule.GoogleCalendarEventIds.Clear();
            schedule.IsSynced = false;
            schedule.SyncError = string.Empty;
            return new(true, "Schedule removed from Google Calendar.");
        }
        catch (Exception ex)
        {
            return new(false, $"Delete failed: {ex.Message}");
        }
    }

    private static async Task SyncSingleOrRecurringAsync(CalendarService service, CustomerSchedule schedule, Customer customer, CancellationToken cancellationToken)
    {
        var recurrence = RRuleBuilder.Build(schedule);
        var visit = schedule.RecurrenceType == ScheduleRecurrenceType.None
            ? FindVisitComment(schedule, schedule.StartDateTime)
            : null;
        var evt = BuildEvent(schedule, customer, recurrence, visit);
        Event result;
        if (!string.IsNullOrWhiteSpace(schedule.GoogleCalendarEventId))
        {
            try
            {
                var update = service.Events.Update(evt, "primary", schedule.GoogleCalendarEventId);
                update.SupportsAttachments = true;
                result = await update.ExecuteAsync(cancellationToken);
            }
            catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
            {
                result = await service.Events.Insert(evt, "primary").ExecuteAsync(cancellationToken);
            }
        }
        else
        {
            result = await service.Events.Insert(evt, "primary").ExecuteAsync(cancellationToken);
        }
        schedule.GoogleCalendarEventId = result.Id;
        schedule.GoogleCalendarEventIds.Clear();
    }

    private static async Task SyncSpecificDatesAsync(CalendarService service, CustomerSchedule schedule, Customer customer, CancellationToken cancellationToken)
    {
        var existingIds = new Queue<string>(schedule.GoogleCalendarEventIds.Where(id => !string.IsNullOrWhiteSpace(id)));
        var newIds = new List<string>();
        foreach (var date in schedule.SpecificDates.Distinct().OrderBy(d => d.Date))
        {
            var occurrence = date.Date + schedule.StartDateTime.TimeOfDay;
            var evt = BuildEventForDate(schedule, customer, date, FindVisitComment(schedule, occurrence));
            Event result;
            if (existingIds.TryDequeue(out var existingId))
            {
                try
                {
                    var update = service.Events.Update(evt, "primary", existingId);
                    update.SupportsAttachments = true;
                    result = await update.ExecuteAsync(cancellationToken);
                }
                catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    result = await service.Events.Insert(evt, "primary").ExecuteAsync(cancellationToken);
                }
            }
            else
            {
                result = await service.Events.Insert(evt, "primary").ExecuteAsync(cancellationToken);
            }
            newIds.Add(result.Id);
        }

        while (existingIds.TryDequeue(out var leftover))
        {
            try
            {
                await service.Events.Delete("primary", leftover).ExecuteAsync(cancellationToken);
            }
            catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
            {
            }
        }
        schedule.GoogleCalendarEventIds = newIds;
        schedule.GoogleCalendarEventId = string.Empty;
    }

    private static async Task<int> ImportCommentsAsync(CalendarService service, DriveService driveService, CustomerSchedule schedule, Customer customer, CancellationToken cancellationToken)
    {
        var imported = 0;
        foreach (var evt in await GetOccurrenceEventsAsync(service, schedule, cancellationToken))
        {
            var occurrence = GetOccurrenceDateTime(evt);
            if (!occurrence.HasValue)
                continue;
            var remoteComments = CalendarDescriptionFormatter.ParseComments(evt.Description);
            var remoteUpdated = evt.UpdatedDateTimeOffset?.LocalDateTime;
            var local = FindVisitComment(schedule, occurrence.Value);
            var hasRemotePhotos = evt.Attachments?.Any(a => !string.IsNullOrWhiteSpace(a.FileId)) == true;
            if (local == null && (!string.IsNullOrWhiteSpace(remoteComments) || hasRemotePhotos))
            {
                local = new VisitComment
                {
                    OccurrenceDateTime = occurrence.Value,
                    Comments = remoteComments,
                    UpdatedAt = remoteUpdated ?? DateTime.Now,
                    GoogleUpdatedAt = remoteUpdated,
                    GoogleCalendarEventId = evt.Id
                };
                schedule.VisitComments.Add(local);
                if (!string.IsNullOrWhiteSpace(remoteComments))
                {
                    CustomerService.SaveVisitCommentsSnapshot(customer, occurrence.Value, remoteComments);
                    imported++;
                }
            }
            else if (local != null && local.GoogleUpdatedAt != remoteUpdated && CalendarDescriptionFormatter.ShouldImport(local.UpdatedAt, remoteUpdated))
            {
                local.Comments = remoteComments;
                local.UpdatedAt = remoteUpdated ?? DateTime.Now;
                local.GoogleUpdatedAt = remoteUpdated;
                local.GoogleCalendarEventId = evt.Id;
                CustomerService.SaveVisitCommentsSnapshot(customer, occurrence.Value, remoteComments);
                imported++;
            }
            else if (local != null)
            {
                local.GoogleCalendarEventId = evt.Id;
            }

            if (local != null)
                imported += await ImportPhotosAsync(driveService, evt, local, customer, occurrence.Value, cancellationToken);
        }
        return imported;
    }

    private static async Task<int> PushVisitDataAsync(CalendarService service, DriveService driveService, CustomerSchedule schedule, IReadOnlyCollection<Event> events, CancellationToken cancellationToken)
    {
        var uploaded = 0;
        foreach (var comment in schedule.VisitComments)
        {
            var evt = events.FirstOrDefault(item => OccurrencesMatch(GetOccurrenceDateTime(item), comment.OccurrenceDateTime));
            if (evt == null)
                continue;

            var changed = !comment.GoogleUpdatedAt.HasValue || comment.UpdatedAt.ToUniversalTime() > comment.GoogleUpdatedAt.Value.ToUniversalTime();
            if (changed)
                evt.Description = CalendarDescriptionFormatter.Build(evt.Description, comment.Comments);

            var attachments = evt.Attachments?.ToList() ?? [];
            foreach (var photo in comment.Photos)
            {
                if (string.IsNullOrWhiteSpace(photo.DriveFileId) && File.Exists(photo.LocalPath))
                {
                    var driveFile = await UploadPhotoAsync(driveService, photo, cancellationToken);
                    photo.DriveFileId = driveFile.Id;
                    photo.CalendarFileUrl = driveFile.WebViewLink;
                    photo.MimeType = driveFile.MimeType;
                    uploaded++;
                }
                if (string.IsNullOrWhiteSpace(photo.DriveFileId) || attachments.Any(a => a.FileId == photo.DriveFileId))
                    continue;
                attachments.Add(new EventAttachment
                {
                    FileId = photo.DriveFileId,
                    FileUrl = photo.CalendarFileUrl,
                    Title = photo.FileName,
                    MimeType = photo.MimeType
                });
                changed = true;
            }

            if (!changed)
                continue;
            evt.Attachments = attachments
                .GroupBy(a => string.IsNullOrWhiteSpace(a.FileId) ? a.FileUrl : a.FileId)
                .Select(group => group.First())
                .ToList();
            var request = service.Events.Update(evt, "primary", evt.Id);
            request.SupportsAttachments = true;
            var updated = await request.ExecuteAsync(cancellationToken);
            comment.GoogleCalendarEventId = updated.Id;
            comment.GoogleUpdatedAt = updated.UpdatedDateTimeOffset?.LocalDateTime ?? DateTime.Now;
            if (!string.IsNullOrWhiteSpace(comment.Comments))
                uploaded++;
        }
        return uploaded;
    }

    private static async Task<int> ImportPhotosAsync(DriveService driveService, Event evt, VisitComment comment, Customer customer, DateTime occurrence, CancellationToken cancellationToken)
    {
        var imported = 0;
        foreach (var attachment in evt.Attachments ?? [])
        {
            if (string.IsNullOrWhiteSpace(attachment.FileId))
                continue;
            var existing = comment.Photos.FirstOrDefault(p => p.DriveFileId == attachment.FileId);
            if (existing != null && File.Exists(existing.LocalPath))
                continue;

            var metadataRequest = driveService.Files.Get(attachment.FileId);
            metadataRequest.Fields = "id,name,mimeType,webViewLink";
            var metadata = await metadataRequest.ExecuteAsync(cancellationToken);
            if (metadata.MimeType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) != true)
                continue;
            using var content = new MemoryStream();
            var progress = await driveService.Files.Get(attachment.FileId).DownloadAsync(content, cancellationToken);
            if (progress.Status != DownloadStatus.Completed)
                continue;
            content.Position = 0;
            var localPath = CustomerService.SaveDownloadedVisitPhoto(customer, occurrence, metadata.Name ?? attachment.Title ?? "Photo", content);
            if (existing == null)
            {
                comment.Photos.Add(new VisitPhoto
                {
                    FileName = Path.GetFileName(localPath),
                    LocalPath = localPath,
                    DriveFileId = attachment.FileId,
                    CalendarFileUrl = attachment.FileUrl ?? metadata.WebViewLink,
                    MimeType = attachment.MimeType ?? metadata.MimeType,
                    AddedAt = DateTime.Now
                });
            }
            else
            {
                existing.LocalPath = localPath;
                existing.FileName = Path.GetFileName(localPath);
            }
            imported++;
        }
        return imported;
    }

    private static async Task<DriveFile> UploadPhotoAsync(DriveService driveService, VisitPhoto photo, CancellationToken cancellationToken)
    {
        var mimeType = string.IsNullOrWhiteSpace(photo.MimeType) ? GetImageMimeType(photo.LocalPath) : photo.MimeType;
        await using var content = File.OpenRead(photo.LocalPath);
        var request = driveService.Files.Create(new DriveFile { Name = photo.FileName }, content, mimeType);
        request.Fields = "id,name,mimeType,webViewLink";
        var progress = await request.UploadAsync(cancellationToken);
        if (progress.Status != UploadStatus.Completed || request.ResponseBody == null)
            throw new InvalidOperationException($"Could not upload {photo.FileName} to Google Drive: {progress.Exception?.Message}");
        return request.ResponseBody;
    }

    private static string GetImageMimeType(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".gif" => "image/gif",
        ".webp" => "image/webp",
        ".heic" => "image/heic",
        _ => "image/jpeg"
    };

    private static async Task<List<Event>> GetOccurrenceEventsAsync(CalendarService service, CustomerSchedule schedule, CancellationToken cancellationToken)
    {
        var result = new List<Event>();
        if (schedule.RecurrenceType == ScheduleRecurrenceType.SpecificDates)
        {
            foreach (var id in schedule.GoogleCalendarEventIds.Where(id => !string.IsNullOrWhiteSpace(id)))
            {
                try
                {
                    result.Add(await service.Events.Get("primary", id).ExecuteAsync(cancellationToken));
                }
                catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
                {
                }
            }
            return result;
        }

        if (string.IsNullOrWhiteSpace(schedule.GoogleCalendarEventId))
            return result;
        if (schedule.RecurrenceType == ScheduleRecurrenceType.None)
        {
            try
            {
                result.Add(await service.Events.Get("primary", schedule.GoogleCalendarEventId).ExecuteAsync(cancellationToken));
            }
            catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
            {
            }
            return result;
        }

        var latestComment = schedule.VisitComments.Select(c => c.OccurrenceDateTime).DefaultIfEmpty(DateTime.Today).Max();
        var request = service.Events.Instances("primary", schedule.GoogleCalendarEventId);
        request.TimeMinDateTimeOffset = new DateTimeOffset(schedule.StartDateTime.Date.AddDays(-1));
        request.TimeMaxDateTimeOffset = new DateTimeOffset(new[] { DateTime.Today.AddDays(2), latestComment.AddDays(2) }.Max());
        request.ShowDeleted = false;
        string? pageToken = null;
        do
        {
            request.PageToken = pageToken;
            var page = await request.ExecuteAsync(cancellationToken);
            if (page.Items != null)
                result.AddRange(page.Items);
            pageToken = page.NextPageToken;
        } while (!string.IsNullOrWhiteSpace(pageToken));
        return result;
    }

    private static Event BuildEvent(CustomerSchedule schedule, Customer customer, string? recurrence, VisitComment? visit)
    {
        var evt = new Event
        {
            Summary = BuildSummary(schedule, customer),
            Description = CalendarDescriptionFormatter.Build(BuildBaseDescription(schedule, customer), visit?.Comments),
            Location = schedule.Location,
            Start = ToEventDateTime(schedule.StartDateTime),
            End = ToEventDateTime(schedule.EndDateTime),
            Reminders = BuildReminders(schedule.ReminderMinutesBefore),
            Attachments = BuildAttachments(visit)
        };
        if (!string.IsNullOrWhiteSpace(recurrence))
            evt.Recurrence = [recurrence];
        return evt;
    }

    private static Event BuildEventForDate(CustomerSchedule schedule, Customer customer, DateTime date, VisitComment? visit)
    {
        var start = date.Date + schedule.StartDateTime.TimeOfDay;
        var end = date.Date + schedule.EndDateTime.TimeOfDay;
        return new Event
        {
            Summary = BuildSummary(schedule, customer),
            Description = CalendarDescriptionFormatter.Build(BuildBaseDescription(schedule, customer), visit?.Comments),
            Location = schedule.Location,
            Start = ToEventDateTime(start),
            End = ToEventDateTime(end),
            Reminders = BuildReminders(schedule.ReminderMinutesBefore),
            Attachments = BuildAttachments(visit)
        };
    }

    private static IList<EventAttachment>? BuildAttachments(VisitComment? visit)
    {
        var attachments = visit?.Photos
            .Where(photo => !string.IsNullOrWhiteSpace(photo.DriveFileId) && !string.IsNullOrWhiteSpace(photo.CalendarFileUrl))
            .Select(photo => new EventAttachment
            {
                FileId = photo.DriveFileId,
                FileUrl = photo.CalendarFileUrl,
                Title = photo.FileName,
                MimeType = photo.MimeType
            })
            .ToList();
        return attachments?.Count > 0 ? attachments : null;
    }

    private static string BuildSummary(CustomerSchedule schedule, Customer customer)
    {
        var customerName = string.IsNullOrWhiteSpace(customer.Name) ? "Customer" : customer.Name;
        var title = string.IsNullOrWhiteSpace(schedule.Title) ? "Scheduled visit" : schedule.Title;
        return $"{title} - {customerName}";
    }

    private static string BuildBaseDescription(CustomerSchedule schedule, Customer customer)
    {
        var lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(schedule.Description)) lines.Add(schedule.Description);
        if (!string.IsNullOrWhiteSpace(customer.Phone)) lines.Add($"Phone: {customer.Phone}");
        if (!string.IsNullOrWhiteSpace(customer.Email)) lines.Add($"Email: {customer.Email}");
        if (!string.IsNullOrWhiteSpace(customer.Address)) lines.Add($"Address: {customer.Address}");
        return string.Join("\n", lines);
    }

    private static VisitComment? FindVisitComment(CustomerSchedule schedule, DateTime occurrence) =>
        schedule.VisitComments.FirstOrDefault(comment => OccurrencesMatch(comment.OccurrenceDateTime, occurrence));

    private static bool OccurrencesMatch(DateTime? left, DateTime right) =>
        left.HasValue && Math.Abs((left.Value - right).TotalMinutes) < 1;

    private static DateTime? GetOccurrenceDateTime(Event evt)
    {
        var eventTime = evt.OriginalStartTime ?? evt.Start;
        if (eventTime?.DateTimeDateTimeOffset is DateTimeOffset value)
            return value.LocalDateTime;
        if (DateTime.TryParse(eventTime?.Date, out var date))
            return date;
        return null;
    }

    private static Event.RemindersData BuildReminders(int minutesBefore) => new()
    {
        UseDefault = false,
        Overrides = [new EventReminder { Method = "popup", Minutes = Math.Max(0, minutesBefore) }]
    };

    private static EventDateTime ToEventDateTime(DateTime dateTime) => new()
    {
        DateTimeDateTimeOffset = dateTime,
        TimeZone = GetIanaTimeZoneId()
    };

    private static string GetIanaTimeZoneId()
    {
        var localId = TimeZoneInfo.Local.Id;
        return TimeZoneInfo.TryConvertWindowsIdToIanaId(localId, out var ianaId) ? ianaId : "UTC";
    }

    private static CalendarService CreateService(Google.Apis.Auth.OAuth2.UserCredential credential) => new(new BaseClientService.Initializer
    {
        HttpClientInitializer = credential,
        ApplicationName = ApplicationName
    });

    private static DriveService CreateDriveService(Google.Apis.Auth.OAuth2.UserCredential credential) => new(new BaseClientService.Initializer
    {
        HttpClientInitializer = credential,
        ApplicationName = ApplicationName
    });
}

public sealed record GoogleCalendarStatus(bool IsConfigured, bool IsConnected, string Message);
public sealed record SyncResult(bool Success, string Message, int ImportedComments = 0, int UploadedComments = 0);
