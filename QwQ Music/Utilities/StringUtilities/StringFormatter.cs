using System;

namespace QwQ_Music.Utilities.StringUtilities;

/// <summary>
/// 字符串格式化工具类
/// </summary>
public static class StringFormatter
{
    /// <summary>
    /// 格式化文件大小
    /// </summary>
    /// <param name="bytes">字节数</param>
    /// <param name="decimalPlaces">小数位数</param>
    /// <returns>格式化后的文件大小字符串</returns>
    public static string FormatFileSize(long bytes, int decimalPlaces = 2)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB", "PB"];
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return string.Format("{{0:F" + decimalPlaces + "}} {1}", len, sizes[order]);
    }

    /// <summary>
    /// 格式化时间间隔
    /// </summary>
    /// <param name="timeSpan">时间间隔</param>
    /// <param name="showSeconds">是否显示秒数</param>
    /// <returns>格式化后的时间字符串</returns>
    public static string FormatTimeSpan(TimeSpan timeSpan, bool showSeconds = true)
    {
        if (timeSpan.TotalHours >= 1)
        {
            return showSeconds
                ? $"{(int)timeSpan.TotalHours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}"
                : $"{(int)timeSpan.TotalHours:D2}:{timeSpan.Minutes:D2}";
        }

        return showSeconds ? $"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}" : $"{timeSpan.Minutes:D2}";
    }

    /// <summary>
    /// 格式化数字，添加千位分隔符
    /// </summary>
    /// <param name="number">数字</param>
    /// <returns>格式化后的数字字符串</returns>
    public static string FormatNumber(long number)
    {
        return number.ToString("N0");
    }

    /// <summary>
    /// 格式化百分比
    /// </summary>
    /// <param name="value">值</param>
    /// <param name="total">总值</param>
    /// <param name="decimalPlaces">小数位数</param>
    /// <returns>格式化后的百分比字符串</returns>
    public static string FormatPercentage(double value, double total, int decimalPlaces = 1)
    {
        if (total == 0)
            return "0%";
        double percentage = (value / total) * 100;
        return string.Format("{{0:F" + decimalPlaces + "}}%", percentage);
    }
}