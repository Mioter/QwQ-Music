using System.Globalization;
using Avalonia.Data.Converters;
using SoundFlow.Backends.MiniAudio.Enums;

namespace QwQ_Music.UI.Converters;

public class ShareModeToBoolConverter : IValueConverter {
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (value is ShareMode shareMode)
            return shareMode == ShareMode.Exclusive;

        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (value is bool boolValue)
            return boolValue ? ShareMode.Exclusive : ShareMode.Shared;

        return ShareMode.Shared;
    }
}

public class BoolToShareModeConverter : IValueConverter {
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (value is ShareMode shareMode)
            return shareMode == ShareMode.Shared;

        return true;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (value is bool boolValue)
            return boolValue ? ShareMode.Shared : ShareMode.Exclusive;

        return ShareMode.Exclusive;
    }
}