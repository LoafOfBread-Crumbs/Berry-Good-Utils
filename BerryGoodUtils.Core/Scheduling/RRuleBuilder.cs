using System.Text;
using BerryGoodUtils.Models;

namespace BerryGoodUtils.Core.Scheduling;

/// <summary>
/// Builds iCalendar RRULE strings for Google Calendar recurring events.
/// </summary>
public static class RRuleBuilder
{
    /// <summary>
    /// Returns a single RRULE string for schedules that can be expressed as one recurring event.
    /// Returns null for non-recurring schedules or specific-dates schedules.
    /// </summary>
    public static string? Build(CustomerSchedule schedule)
    {
        if (schedule.RecurrenceType == ScheduleRecurrenceType.None ||
            schedule.RecurrenceType == ScheduleRecurrenceType.SpecificDates)
        {
            return null;
        }

        var sb = new StringBuilder("RRULE:FREQ=");
        switch (schedule.RecurrenceType)
        {
            case ScheduleRecurrenceType.Weekly:
                sb.Append("WEEKLY");
                break;
            case ScheduleRecurrenceType.BiWeekly:
                sb.Append("WEEKLY;INTERVAL=2");
                break;
            case ScheduleRecurrenceType.Monthly:
                sb.Append("MONTHLY");
                break;
            case ScheduleRecurrenceType.Yearly:
                sb.Append("YEARLY");
                break;
            case ScheduleRecurrenceType.CustomInterval:
                AppendCustomInterval(schedule, sb);
                break;
        }

        return sb.ToString();
    }

    private static void AppendCustomInterval(CustomerSchedule schedule, StringBuilder sb)
    {
        var freq = schedule.CustomIntervalUnit switch
        {
            CustomIntervalUnit.Days => "DAILY",
            CustomIntervalUnit.Weeks => "WEEKLY",
            CustomIntervalUnit.Months => "MONTHLY",
            CustomIntervalUnit.Years => "YEARLY",
            _ => "DAILY"
        };
        sb.Append(freq);
        if (schedule.CustomIntervalValue > 1)
            sb.Append($";INTERVAL={schedule.CustomIntervalValue}");
    }
}
