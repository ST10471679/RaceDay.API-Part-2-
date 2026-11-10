using System.Globalization;

namespace RaceDay.API.Services;

/// <summary>
/// Parses and formats race finish times. "45:30" means 45 minutes 30 seconds
/// (plain TimeSpan.Parse would read it as 45 HOURS 30 minutes).
/// </summary>
public static class ResultTimeFormatter
{
    private static readonly string[] Formats =
    {
        @"h\:mm\:ss", @"hh\:mm\:ss", @"h\:mm\:ss\.f", @"h\:mm\:ss\.ff", @"h\:mm\:ss\.fff",
        @"m\:ss", @"mm\:ss"
    };

    public static bool TryParse(string? input, out TimeSpan time)
    {
        time = TimeSpan.Zero;
        if (string.IsNullOrWhiteSpace(input)) return false;

        return TimeSpan.TryParseExact(input.Trim(), Formats, CultureInfo.InvariantCulture, out time)
               && time > TimeSpan.Zero;
    }

    public static string Format(TimeSpan time) =>
        $"{(int)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}";
}
