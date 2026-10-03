namespace BerryGoodUtils.Core.Scheduling;

public static class CalendarDescriptionFormatter
{
    public const string StartMarker = "--- POST-VISIT COMMENTS ---";
    public const string EndMarker = "--- END POST-VISIT COMMENTS ---";

    public static string Build(string? description, string? comments)
    {
        var baseDescription = RemoveCommentsSection(description).TrimEnd();
        var section = $"{StartMarker}\n{comments?.Trim() ?? string.Empty}\n{EndMarker}";
        return string.IsNullOrWhiteSpace(baseDescription) ? section : $"{baseDescription}\n\n{section}";
    }

    public static string ParseComments(string? description)
    {
        if (string.IsNullOrEmpty(description))
            return string.Empty;

        var normalized = NormalizeLineEndings(description);
        var start = normalized.IndexOf(StartMarker, StringComparison.Ordinal);
        if (start < 0)
            return string.Empty;

        start += StartMarker.Length;
        var end = normalized.IndexOf(EndMarker, start, StringComparison.Ordinal);
        if (end < 0)
            return string.Empty;

        return normalized[start..end].Trim();
    }

    public static string RemoveCommentsSection(string? description)
    {
        if (string.IsNullOrEmpty(description))
            return string.Empty;

        var normalized = NormalizeLineEndings(description);
        var start = normalized.IndexOf(StartMarker, StringComparison.Ordinal);
        if (start < 0)
            return normalized;

        var end = normalized.IndexOf(EndMarker, start + StartMarker.Length, StringComparison.Ordinal);
        if (end < 0)
            return normalized;

        end += EndMarker.Length;
        return (normalized[..start] + normalized[end..]).Trim();
    }

    public static bool ShouldImport(DateTime localUpdatedAt, DateTime? googleUpdatedAt)
    {
        return googleUpdatedAt.HasValue && googleUpdatedAt.Value.ToUniversalTime() > localUpdatedAt.ToUniversalTime();
    }

    private static string NormalizeLineEndings(string value) => value.Replace("\r\n", "\n").Replace('\r', '\n');
}
