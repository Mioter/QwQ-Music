using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace QwQ_Music.Helper.Converters;

public class StringEqualityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // 将值和参数统一转换为字符串（处理 null 情况）
        string valueStr = value?.ToString() ?? string.Empty;
        string paramStr = parameter?.ToString() ?? string.Empty;

        // 使用 Ordinal 比较（区分大小写）
        return string.Equals(valueStr, paramStr, StringComparison.Ordinal);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // 通常不需要反向转换，直接返回未处理
        return value;
    }
}
