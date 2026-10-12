using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using QwQ_Music.Common.Services;

namespace QwQ_Music.UI.Converters;

public class TranslationConverter : IValueConverter {
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        return I18NService.Lang.Translation[value!.ToString()!, value.GetType().Name];
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        return AvaloniaProperty.UnsetValue;
    }
}