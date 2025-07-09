using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace QwQ_Music.Helper.Converters;

public class ValueComparisonConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return false;

        string expression = parameter.ToString() ?? string.Empty;
        if (string.IsNullOrEmpty(expression))
            return false;

        try
        {
            if (!double.TryParse(value.ToString(), out double currentValue))
                return false;

            // 分割表达式为多个比较部分
            string[] parts = expression.Split([' '], StringSplitOptions.RemoveEmptyEntries);
            return parts.Length switch
            {
                < 3 => false,
                3 => EvaluateSingleComparison(parts, currentValue), // 处理单个比较表达式
                _ => EvaluateRangeComparison(parts, currentValue), // 处理范围比较表达式
            };
        }
        catch
        {
            return false;
        }
    }

    private static bool EvaluateSingleComparison(string[] parts, double currentValue)
    {
        if (parts[0] == "@VALUE")
        {
            if (!double.TryParse(parts[2], out double compareValue))
                return false;

            return parts[1] switch
            {
                ">" => currentValue > compareValue,
                "<" => currentValue < compareValue,
                "==" => Math.Abs(currentValue - compareValue) < double.Epsilon,
                "!=" => Math.Abs(currentValue - compareValue) > double.Epsilon,
                ">=" => currentValue >= compareValue,
                "<=" => currentValue <= compareValue,
                _ => false,
            };
        }

        if (parts[2] != "@VALUE")
            return false;
        {
            if (!double.TryParse(parts[0], out double compareValue))
                return false;

            return parts[1] switch
            {
                ">" => compareValue > currentValue,
                "<" => compareValue < currentValue,
                "==" => Math.Abs(compareValue - currentValue) < double.Epsilon,
                "!=" => Math.Abs(compareValue - currentValue) > double.Epsilon,
                ">=" => compareValue >= currentValue,
                "<=" => compareValue <= currentValue,
                _ => false,
            };
        }
    }

    private static bool EvaluateRangeComparison(string[] parts, double currentValue)
    {
        // 检查是否是有效的范围表达式
        if (parts.Length != 5)
            return false;

        // 解析第一个比较
        if (!double.TryParse(parts[0], out double lowerBound))
            return false;

        // 解析第二个比较
        if (!double.TryParse(parts[4], out double upperBound))
            return false;

        // 检查操作符
        if (parts[1] != "<" || parts[3] != "<")
            return false;

        // 检查@VALUE的位置
        if (parts[2] != "@VALUE")
            return false;

        // 执行范围检查
        return currentValue > lowerBound && currentValue < upperBound;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
