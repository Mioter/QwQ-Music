using System;
using System.Linq;

namespace QwQ_Music.Common.Utilities.StringUtilities;

/// <summary>
///     字符串乱序工具类
/// </summary>
public static class StringShuffler
{
    private static readonly Random _random = new();
    private static readonly char[] _separator = [' ', '\t', '\n', '\r'];

    /// <summary>
    ///     随机打乱字符串中的字符顺序
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <returns>乱序后的字符串</returns>
    public static string Shuffle(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        char[] chars = input.ToCharArray();

        for (int i = chars.Length - 1; i > 0; i--)
        {
            int j = _random.Next(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars);
    }

    /// <summary>
    ///     按单词乱序（保持单词完整性）
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <returns>单词乱序后的字符串</returns>
    public static string ShuffleWords(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        string[] words = input.Split(_separator, StringSplitOptions.RemoveEmptyEntries);
        string[] shuffledWords = words.OrderBy(x => _random.Next()).ToArray();

        return string.Join(" ", shuffledWords);
    }

    /// <summary>
    ///     按行乱序
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <returns>行乱序后的字符串</returns>
    public static string ShuffleLines(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        string[] lines = input.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
        string[] shuffledLines = lines.OrderBy(x => _random.Next()).ToArray();

        return string.Join(Environment.NewLine, shuffledLines);
    }
}
