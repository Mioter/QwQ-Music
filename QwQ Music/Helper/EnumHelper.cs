using System;
using System.Collections.Generic;
using System.Linq;

namespace QwQ_Music.Helper;

public class EnumHelper<T>
{
    public static List<T> ToList()
    {
        return Enum.GetValues(typeof(T)).Cast<T>().ToList();
    }
    
    public static T[] ToArray()
    {
        return Enum.GetValues(typeof(T)).Cast<T>().ToArray();
    }
}
