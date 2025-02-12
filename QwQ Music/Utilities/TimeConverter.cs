using System;
using System.Linq;

namespace QwQ_Music.Utilities;

public static class TimeConverter
{
    public static string FormatSeconds(this double totalSeconds, int decimalPlaces = 3)
    {
        if (decimalPlaces < 0)
            throw new ArgumentOutOfRangeException(nameof(decimalPlaces), "Decimal places must be non-negative.");

        if (totalSeconds < 0)
            return "00:00";

        int hours = (int)(totalSeconds / 3600);
        double remainingAfterHours = totalSeconds % 3600;
        int minutes = (int)(remainingAfterHours / 60);
        double seconds = remainingAfterHours % 60;

        string secondsFormat = decimalPlaces > 0 ? $"00.{new string('#', decimalPlaces)}" : "00";
        string formattedSeconds = seconds.ToString(secondsFormat);

        return hours > 0
            ? $"{hours:D2}:{minutes:D2}:{formattedSeconds}"
            : $"{minutes:D2}:{formattedSeconds}";
    }

    public static double ParseSeconds(this string? timeString)
    {
        if (string.IsNullOrWhiteSpace(timeString))
            return 0;

        timeString = timeString.Trim();
        int colonCount = timeString.Count(c => c == ':');

        switch (colonCount)
        {
            case 0:
                // 输入是秒数（带或不带小数）
                if (double.TryParse(timeString, out double seconds))
                    return seconds;
                break;
            case 1:
                // 处理 mm:ss 或 ss.mm 格式
                string[] parts = timeString.Split(':');
                if (parts.Length == 2 &&
                    double.TryParse(parts[0], out double minutes) &&
                    double.TryParse(parts[1], out double secs))
                {
                    return minutes * 60 + secs;
                }
                break;
            case 2:
                // 尝试使用 TimeSpan 解析 hh:mm:ss 或 hh:mm:ss.ff 格式
                if (TimeSpan.TryParse(timeString, out var timeSpan))
                    return timeSpan.TotalSeconds;
                break;
        }

        throw new FormatException($"Invalid time format: {timeString}");
    }
}
