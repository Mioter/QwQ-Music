using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;

namespace QwQ_Music.Converter;

public class PriorityBoolMultiConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        // 解析参数字符串为列表
        string? paramList = parameter as string;
        if (string.IsNullOrEmpty(paramList))
            return double.NaN;

        // 将参数字符串转换为 double 数组
        double[] parameters = paramList
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(double.Parse)
            .ToArray();

        // 优先处理 values[0] 为 true 的情况
        if (values.Count > 0 && values[0] is true)
        {
            return parameters.Length > 0 ? parameters[0] : double.NaN;
        }

        // 查找最后一个为 true 的索引
        int lastTrueIndex = -1;
        for (int i = 1; i < values.Count; i++)
        {
            if (values[i] is true)
            {
                lastTrueIndex = i;
            }
        }

        // 返回对应参数或默认值
        if (lastTrueIndex != -1 && parameters.Length > lastTrueIndex)
            return parameters[lastTrueIndex];

        return parameters.Length > 0 ? parameters[0] : double.NaN; // 所有值为 false 时返回第一个参数
    }
}
