using System;
using System.Collections.Generic;
using System.Linq;

namespace QwQ_Music.Helper;

public static class EnumHelper<T>
{
    public static List<T> ToList()
    {
        return GeuEnumerable().ToList();
    }

    public static T[] ToArray()
    {
        return GeuEnumerable().ToArray();
    }

    private static IEnumerable<T> GeuEnumerable()
    {
        return Enum.GetValuesAsUnderlyingType(typeof(T)).Cast<T>();
    }
}
