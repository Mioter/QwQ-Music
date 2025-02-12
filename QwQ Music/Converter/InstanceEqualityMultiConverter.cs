using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

namespace QwQ_Music.Converter;

public class InstanceEqualityMultiConverter : IMultiValueConverter
{

    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count != 2 || values[0] == null || values[1] == null)
        {
            return false;
        }

        // 比较两个对象是否相同
        return Equals(values[0], values[1]);
    }
}
