using BerryGoodUtils.Models;

namespace BerryGoodUtils.Core.Scheduling;

/// <summary>
/// Expands a schedule into individual local occurrence dates used by the in-app calendar UI.
/// </summary>
public static class ScheduleOccurrenceCalculator
{
    /// <summary>
    /// Gets the occurrences of a schedule that fall within the requested date range (inclusive).
    /// For SpecificDates schedules, returns matching dates. For recurring schedules, expands based on the recurrence rule.
    /// </summary>
    public static IReadOnlyList<DateTime> GetOccurrences(CustomerSchedule schedule, DateTime start, DateTime end)
    {
        if (schedule.RecurrenceType == ScheduleRecurrenceType.SpecificDates)
        {
            return schedule.SpecificDates
                .Where(d => d.Date >= start.Date && d.Date <= end.Date)
                .Select(d => d.Date)
                .Distinct()
                .OrderBy(d => d)
                .ToList();
        }

        if (schedule.RecurrenceType == ScheduleRecurrenceType.None)
        {
            var single = schedule.StartDateTime.Date;
            return single >= start.Date && single <= end.Date ? [single] : Array.Empty<DateTime>();
        }

        return ExpandRecurring(schedule, start, end).ToList();
    }

    /// <summary>
    /// Returns the first upcoming occurrence date on or after the given reference date.
    /// </summary>
    public static DateTime? GetNextOccurrence(CustomerSchedule schedule, DateTime? reference = null)
    {
        var from = (reference ?? DateTime.Now).Date;

        if (schedule.RecurrenceType == ScheduleRecurrenceType.SpecificDates)
        {
            return schedule.SpecificDates
                .Select(d => d.Date)
                .Where(d => d >= from)
                .OrderBy(d => d)
                .FirstOrDefault();
        }

        if (schedule.RecurrenceType == ScheduleRecurrenceType.None)
        {
            var single = schedule.StartDateTime.Date;
            return single >= from ? single : null;
        }

        return ExpandRecurring(schedule, from, from.AddYears(2)).FirstOrDefault();
    }

    private static IEnumerable<DateTime> ExpandRecurring(CustomerSchedule schedule, DateTime start, DateTime end)
    {
        var current = schedule.StartDateTime.Date;
        if (current > end.Date)
            yield break;

        while (current <= end.Date)
        {
            if (current >= start.Date)
                yield return current;

            current = StepForward(schedule, current);

            // Safety guard to avoid infinite loops from invalid intervals.
            if (current <= schedule.StartDateTime.Date.AddDays(-1))
                yield break;
        }
    }

    private static DateTime StepForward(CustomerSchedule schedule, DateTime current)
    {
        switch (schedule.RecurrenceType)
        {
            case ScheduleRecurrenceType.Weekly:
                return current.AddDays(7);
            case ScheduleRecurrenceType.BiWeekly:
                return current.AddDays(14);
            case ScheduleRecurrenceType.Monthly:
                return current.AddMonths(1);
            case ScheduleRecurrenceType.Yearly:
                return current.AddYears(1);
            case ScheduleRecurrenceType.CustomInterval:
            {
                var value = Math.Max(1, schedule.CustomIntervalValue);
                return schedule.CustomIntervalUnit switch
                {
                    CustomIntervalUnit.Days => current.AddDays(value),
                    CustomIntervalUnit.Weeks => current.AddDays(value * 7),
                    CustomIntervalUnit.Months => current.AddMonths(value),
                    CustomIntervalUnit.Years => current.AddYears(value),
                    _ => current.AddDays(value)
                };
            }
            default:
                return DateTime.MaxValue.Date;
        }
    }
}
