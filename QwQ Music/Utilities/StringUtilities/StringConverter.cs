using System;
using System.Text;
using System.Text.RegularExpressions;

namespace QwQ_Music.Utilities.StringUtilities;

/// <summary>
/// 字符串转换工具类
/// </summary>
public static partial class StringConverter
{
    /// <summary>
    /// 将字符串转换为驼峰命名
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <returns>驼峰命名字符串</returns>
    public static string ToCamelCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        string[] words = input.Split([' ', '_', '-'], StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
            return input;

        var result = new StringBuilder();
        result.Append(char.ToLowerInvariant(words[0][0]));
        result.Append(words[0][1..].ToLowerInvariant());

        for (int i = 1; i < words.Length; i++)
        {
            if (words[i].Length <= 0)
                continue;
            result.Append(char.ToUpperInvariant(words[i][0]));
            result.Append(words[i][1..].ToLowerInvariant());
        }

        return result.ToString();
    }

    /// <summary>
    /// 将字符串转换为帕斯卡命名
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <returns>帕斯卡命名字符串</returns>
    public static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        string[] words = input.Split([' ', '_', '-'], StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
            return input;

        var result = new StringBuilder();
        foreach (string word in words)
        {
            if (word.Length <= 0)
                continue;

            result.Append(char.ToUpperInvariant(word[0]));
            result.Append(word[1..].ToLowerInvariant());
        }

        return result.ToString();
    }

    /// <summary>
    /// 将字符串转换为蛇形命名
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <returns>蛇形命名字符串</returns>
    public static string ToSnakeCase(string input)
    {
        return string.IsNullOrEmpty(input) ? input : PascalCaseRegex().Replace(input, "$1_$2").ToLowerInvariant();
    }

    /// <summary>
    /// 将字符串转换为短横线命名
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <returns>短横线命名字符串</returns>
    public static string ToKebabCase(string input)
    {
        return string.IsNullOrEmpty(input) ? input : PascalCaseRegex().Replace(input, "$1-$2").ToLowerInvariant();
    }

    [GeneratedRegex(@"([a-z])([A-Z])")]
    private static partial Regex PascalCaseRegex();
}
