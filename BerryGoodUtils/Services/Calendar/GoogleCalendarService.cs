using BerryGoodUtils.Core.Scheduling;
using BerryGoodUtils.Models;
using BerryGoodUtils.Services.Auth;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;

namespace BerryGoodUtils.Services.Calendar;

/// <summary>
/// Syncs customer schedules to the signed-in user's Google Calendar.
/// Events are created in the primary calendar and include reminders so mobile Google Calendar apps alert.
/// </summary>
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

            if (schedule.RecurrenceType == ScheduleRecurrenceType.SpecificDates)
            {
                return await SyncSpecificDatesAsync(service, schedule, customer, cancellationToken);
            }

            var recurrence = RRuleBuilder.Build(schedule);
            var evt = BuildEvent(schedule, customer, recurrence);

            Event result;
            if (!string.IsNullOrWhiteSpace(schedule.GoogleCalendarEventId))
            {
                result = await service.Events.Update(evt, "primary", schedule.GoogleCalendarEventId).ExecuteAsync(cancellationToken);
            }
            else
            {
                result = await service.Events.Insert(evt, "primary").ExecuteAsync(cancellationToken);
            }

            schedule.GoogleCalendarEventId = result.Id;
            schedule.GoogleCalendarEventIds.Clear();
            schedule.IsSynced = true;
            schedule.LastSyncedAt = DateTime.Now;
            schedule.SyncError = string.Empty;
            return new(true, "Schedule synced to Google Calendar.");
        }
        catch (Exception ex)
        {
            schedule.IsSynced = false;
            schedule.SyncError = ex.Message;
            return new(false, $"Sync failed: {ex.Message}");
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
                    // Already deleted remotely; continue cleaning up local ids.
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

    private static async Task<SyncResult> SyncSpecificDatesAsync(CalendarService service, CustomerSchedule schedule, Customer customer, CancellationToken cancellationToken)
    {
        var existingIds = new HashSet<string>(schedule.GoogleCalendarEventIds.Where(id => !string.IsNullOrWhiteSpace(id)));
        var newIds = new List<string>();

        foreach (var date in schedule.SpecificDates.Distinct().OrderBy(d => d.Date))
        {
            var evt = BuildEventForDate(schedule, customer, date);
            Event result;

            var existingId = existingIds.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(existingId))
            {
                try
                {
                    result = await service.Events.Update(evt, "primary", existingId).ExecuteAsync(cancellationToken);
                    existingIds.Remove(existingId);
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

        foreach (var leftover in existingIds)
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
        schedule.IsSynced = true;
        schedule.LastSyncedAt = DateTime.Now;
        schedule.SyncError = string.Empty;
        return new(true, $"Synced {newIds.Count} event(s) to Google Calendar.");
    }

    private static Event BuildEvent(CustomerSchedule schedule, Customer customer, string? recurrence)
    {
        var evt = new Event
        {
            Summary = BuildSummary(schedule, customer),
            Description = BuildDescription(schedule, customer),
            Location = schedule.Location,
            Start = ToEventDateTime(schedule.StartDateTime),
            End = ToEventDateTime(schedule.EndDateTime),
            Reminders = BuildReminders(schedule.ReminderMinutesBefore)
        };

        if (!string.IsNullOrWhiteSpace(recurrence))
        {
            evt.Recurrence = new List<string> { recurrence };
        }

        return evt;
    }

    private static Event BuildEventForDate(CustomerSchedule schedule, Customer customer, DateTime date)
    {
        var start = date.Date + schedule.StartDateTime.TimeOfDay;
        var end = date.Date + schedule.EndDateTime.TimeOfDay;
        var evt = new Event
        {
            Summary = BuildSummary(schedule, customer),
            Description = BuildDescription(schedule, customer),
            Location = schedule.Location,
            Start = ToEventDateTime(start),
            End = ToEventDateTime(end),
            Reminders = BuildReminders(schedule.ReminderMinutesBefore)
        };
        return evt;
    }

    private static string BuildSummary(CustomerSchedule schedule, Customer customer)
    {
        var customerName = string.IsNullOrWhiteSpace(customer.Name) ? "Customer" : customer.Name;
        var title = string.IsNullOrWhiteSpace(schedule.Title) ? "Scheduled visit" : schedule.Title;
        return $"{title} - {customerName}";
    }

    private static string BuildDescription(CustomerSchedule schedule, Customer customer)
    {
        var lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(schedule.Description))
            lines.Add(schedule.Description);
        if (!string.IsNullOrWhiteSpace(customer.Phone))
            lines.Add($"Phone: {customer.Phone}");
        if (!string.IsNullOrWhiteSpace(customer.Email))
            lines.Add($"Email: {customer.Email}");
        if (!string.IsNullOrWhiteSpace(customer.Address))
            lines.Add($"Address: {customer.Address}");
        return string.Join("\n", lines);
    }

    private static Event.RemindersData BuildReminders(int minutesBefore)
    {
        minutesBefore = Math.Max(0, minutesBefore);
        return new Event.RemindersData
        {
            UseDefault = false,
            Overrides = new List<EventReminder>
            {
                new() { Method = "popup", Minutes = minutesBefore }
            }
        };
    }

    private static EventDateTime ToEventDateTime(DateTime dateTime)
    {
        return new EventDateTime
        {
            DateTimeDateTimeOffset = dateTime,
            TimeZone = GetIanaTimeZoneId()
        };
    }

    private static string GetIanaTimeZoneId()
    {
        var localId = TimeZoneInfo.Local.Id;
        if (TimeZoneInfo.TryConvertWindowsIdToIanaId(localId, out var ianaId))
            return ianaId;
        return "UTC";
    }

    private static CalendarService CreateService(Google.Apis.Auth.OAuth2.UserCredential credential)
    {
        return new CalendarService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = ApplicationName
        });
    }
}

public sealed record GoogleCalendarStatus(bool IsConfigured, bool IsConnected, string Message);
public sealed record SyncResult(bool Success, string Message);
