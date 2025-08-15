using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using QwQ_Music.Common.Utilities;

namespace QwQ_Music.UI.Converters;

public class SecondsToStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is not double seconds ? throw new NotSupportedException() : seconds.FormatSeconds(parameter is int paramInt ? paramInt : 0);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return AvaloniaProperty.UnsetValue;
    }
}
