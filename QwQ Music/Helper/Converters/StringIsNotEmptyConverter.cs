using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace QwQ_Music.Helper.Converters;

/// <summary>
/// 检查字符串是否为非空或非空白的转换器
/// 如果字符串为null、空或仅包含空白字符，则返回false，否则返回true
/// </summary>
public class StringIsNotEmptyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // 将值转换为字符串（处理null情况）
        string? valueStr = value?.ToString();
        
        // 检查字符串是否为null、空或仅包含空白字符
        return !string.IsNullOrWhiteSpace(valueStr);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // 通常不需要反向转换，直接返回未处理
        throw new NotImplementedException();
    }
}