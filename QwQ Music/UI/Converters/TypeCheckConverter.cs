using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace QwQ_Music.UI.Converters;

public class TypeCheckConverter : IValueConverter {
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (parameter is not Type type)
            throw new InvalidCastException("Must have a parsable type.");
        return value?.GetType() == type;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        return AvaloniaProperty.UnsetValue;
    }
}