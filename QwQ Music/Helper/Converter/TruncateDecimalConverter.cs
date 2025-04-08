using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace QwQ_Music.Helper.Converter;

public class TruncateDecimalConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not TimeSpan timeSpan)
            return string.Empty;

        // 四舍五入到最近的秒（0.5秒向上取整）
        double roundedSeconds = Math.Round(timeSpan.TotalSeconds, MidpointRounding.AwayFromZero);
        timeSpan = TimeSpan.FromSeconds(roundedSeconds);

        return timeSpan.TotalHours >= 2
                ? $"{timeSpan.Days:D2}:{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}"
            : timeSpan.TotalHours >= 1 ? $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}"
            : $"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
