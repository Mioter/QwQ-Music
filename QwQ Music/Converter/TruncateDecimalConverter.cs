using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace QwQ_Music.Converter;

public class TruncateDecimalConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not TimeSpan timeSpan)
            return string.Empty;
        // 截断毫秒部分
        timeSpan = TimeSpan.FromSeconds(Math.Floor(timeSpan.TotalSeconds));

        return timeSpan.TotalHours >= 2
                ? $"{timeSpan.Days:D2}:{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}"
            : timeSpan.TotalHours >= 1 ? $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}" // 格式: 时:分:秒
            : $"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}"; // 格式: 分:秒
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
