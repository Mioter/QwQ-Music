using System;
using System.Linq;

namespace QwQ_Music.Utilities;

public static class TimeConverter
{
    /// <summary>
    /// 将秒数格式化为 hh:mm:ss 或 mm:ss 格式。
    /// </summary>
    /// <param name="totalSeconds">总秒数。</param>
    /// <param name="decimalPlaces">小数位数（默认 3）。</param>
    /// <param name="forceDecimalPadding">是否强制填充小数位（默认 false）。</param>
    /// <returns>格式化后的时间字符串。</returns>
    public static string FormatSeconds(
        this double totalSeconds,
        int decimalPlaces = 3,
        bool forceDecimalPadding = false
        )
    {
        if (decimalPlaces < 0)
            throw new ArgumentOutOfRangeException(nameof(decimalPlaces), "Decimal places must be non-negative.");
        if (totalSeconds < 0)
            return "00:00";

        // 计算小时、分钟和秒
        int hours = (int)(totalSeconds / 3600);
        int minutes = (int)(totalSeconds % 3600 / 60);
        double seconds = totalSeconds % 60;

        // 构建秒数格式字符串
        string secondsFormat = decimalPlaces > 0
            ? $"00.{new string(forceDecimalPadding ? '0' : '#', decimalPlaces)}"
            : "00";

        string formattedSeconds = seconds.ToString(secondsFormat);

        // 返回格式化结果
        return hours > 0
            ? $"{hours:D2}:{minutes:D2}:{formattedSeconds}"
            : $"{minutes:D2}:{formattedSeconds}";
    }

    /// <summary>
    /// 将时间字符串解析为秒数。
    /// </summary>
    /// <param name="timeString">时间字符串。</param>
    /// <returns>解析后的秒数。</returns>
    public static double ParseSeconds(this string? timeString)
    {
        if (string.IsNullOrWhiteSpace(timeString))
            return 0;

        timeString = timeString.Trim();
        int colonCount = timeString.Count(c => c == ':');

        return colonCount switch
        {
            0 => ParseSecondsFromSingleValue(timeString),
            1 => ParseSecondsFromMinutesAndSeconds(timeString),
            2 => ParseSecondsFromTimeSpan(timeString),
            _ => throw new FormatException($"Invalid time format: {timeString}"),
        };
    }

    private static double ParseSecondsFromSingleValue(string timeString)
    {
        return double.TryParse(timeString, out double seconds) ? seconds : 0;
    }

    private static double ParseSecondsFromMinutesAndSeconds(string timeString)
    {
        string[] parts = timeString.Split(':');
        return parts.Length == 2 &&
            double.TryParse(parts[0], out double minutes) &&
            double.TryParse(parts[1], out double seconds)
                ? minutes * 60 + seconds
                : 0;
    }

    private static double ParseSecondsFromTimeSpan(string timeString)
    {
        return TimeSpan.TryParse(timeString, out var timeSpan)
            ? timeSpan.TotalSeconds
            : 0;
    }
}
