using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace QwQ_Music.UI.Converters;

public class EnumTranslationConverter : IValueConverter {
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        return value is null ? AvaloniaProperty.UnsetValue : $"I18N.{value.GetType().Name}.{value}";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        return AvaloniaProperty.UnsetValue;
    }
}