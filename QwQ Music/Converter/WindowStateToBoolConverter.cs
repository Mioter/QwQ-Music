using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace QwQ_Music.Converter;

public class WindowStateToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // 检查输入值是否为 WindowState 类型
        if (value is Avalonia.Controls.WindowState windowState)
        {
            // 判断是否为 Maximized 或 FullScreen
            return windowState is Avalonia.Controls.WindowState.Maximized or Avalonia.Controls.WindowState.FullScreen;
        }

        // 如果输入值无效，返回 false
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
