using System;
using System.Collections.Generic;
using System.Linq;

namespace QwQ_Music.Common.Helper;

public static class EnumDescriptionStore
{
    public static readonly Dictionary<string, string> EnumDescriptions = new()
    {
        // ClosingBehavior
        ["AskAbout"] = "询问",
        ["Exit"] = "直接退出",
        ["HideToTray"] = "隐藏到系统托盘",
    };
}

public static class EnumHelper<T>
    where T : notnull
{
    public static Dictionary<T, string> GetValueDescriptionDictionary()
    {
        return Enum.GetValuesAsUnderlyingType(typeof(T))
            .Cast<T>()
            .ToDictionary(
                e => e,
                e =>
                    EnumDescriptionStore.EnumDescriptions.TryGetValue(e.ToString() ?? "错误枚举", out string? desc)
                        ? desc
                        : e.ToString() ?? "错误枚举"
            );
    }

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
