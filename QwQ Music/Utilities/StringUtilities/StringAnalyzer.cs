using System;
using System.Linq;

namespace QwQ_Music.Utilities.StringUtilities;

/// <summary>
/// 字符串分析统计工具类
/// </summary>
public static class StringAnalyzer
{
    private static readonly char[] _separator = [' ', '\t', '\n', '\r'];

    /// <summary>
    /// 统计字符串中的字符数（不包括空格）
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <returns>字符数</returns>
    public static int CountCharacters(string input)
    {
        return string.IsNullOrEmpty(input) ? 0 : input.Count(c => !char.IsWhiteSpace(c));
    }

    /// <summary>
    /// 统计字符串中的单词数
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <returns>单词数</returns>
    public static int CountWords(string input)
    {
        return string.IsNullOrEmpty(input) ? 0 : input.Split(_separator, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    /// <summary>
    /// 统计字符串中的行数
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <returns>行数</returns>
    public static int CountLines(string input)
    {
        return string.IsNullOrEmpty(input)
            ? 0
            : input.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries).Length;
    }

    /// <summary>
    /// 获取字符串的阅读时间估计（以分钟为单位）
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <param name="wordsPerMinute">每分钟阅读单词数（默认200）</param>
    /// <returns>估计阅读时间（分钟）</returns>
    public static double GetReadingTime(string input, int wordsPerMinute = 200)
    {
        if (string.IsNullOrEmpty(input))
            return 0;

        int wordCount = CountWords(input);
        return Math.Ceiling((double)wordCount / wordsPerMinute);
    }
}