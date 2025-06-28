using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace QwQ_Music.Utilities;

/// <summary>
/// 字符串处理工具类，提供文本乱序、裁剪、格式化等实用功能
/// </summary>
public static partial class StringProcessor
{
    private static readonly Random _random = new();
    private static readonly char[] _separator = [' ', '\t', '\n', '\r'];

    #region 文本乱序功能

    /// <summary>
    /// 随机打乱字符串中的字符顺序
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
    /// 按单词乱序（保持单词完整性）
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
    /// 按行乱序
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

    #endregion

    #region 文本长度裁剪功能

    /// <summary>
    /// 裁剪字符串到指定长度，超出部分用省略号表示
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <param name="maxLength">最大长度</param>
    /// <param name="suffix">后缀（默认为"..."）</param>
    /// <returns>裁剪后的字符串</returns>
    public static string Truncate(string input, int maxLength, string suffix = "...")
    {
        if (string.IsNullOrEmpty(input) || input.Length <= maxLength)
            return input;

        if (maxLength <= suffix.Length)
            return suffix;

        return input[..(maxLength - suffix.Length)] + suffix;
    }

    /// <summary>
    /// 智能裁剪字符串，尝试在单词边界处裁剪
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <param name="maxLength">最大长度</param>
    /// <param name="suffix">后缀（默认为"..."）</param>
    /// <returns>智能裁剪后的字符串</returns>
    public static string TruncateAtWordBoundary(string input, int maxLength, string suffix = "...")
    {
        if (string.IsNullOrEmpty(input) || input.Length <= maxLength)
            return input;

        if (maxLength <= suffix.Length)
            return suffix;

        int targetLength = maxLength - suffix.Length;
        if (targetLength <= 0)
            return suffix;

        // 尝试在单词边界处裁剪
        int lastSpaceIndex = input.LastIndexOf(' ', targetLength);
        if (lastSpaceIndex > targetLength * 0.7) // 如果空格位置合理，在空格处裁剪
        {
            return input[..lastSpaceIndex] + suffix;
        }

        // 否则直接裁剪
        return input[..targetLength] + suffix;
    }

    /// <summary>
    /// 裁剪字符串到指定长度，保持HTML标签完整性
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <param name="maxLength">最大长度</param>
    /// <param name="suffix">后缀（默认为"..."）</param>
    /// <returns>裁剪后的字符串</returns>
    public static string TruncateHtml(string input, int maxLength, string suffix = "...")
    {
        if (string.IsNullOrEmpty(input) || input.Length <= maxLength)
            return input;

        // 简单的HTML标签处理
        var regex = MyRegex();
        var matches = regex.Matches(input);

        int textLength = 0;
        int currentIndex = 0;

        foreach (Match match in matches)
        {
            if (match.Index > currentIndex)
            {
                textLength += match.Index - currentIndex;
                if (textLength >= maxLength)
                    break;
            }
            currentIndex = match.Index + match.Length;
        }

        if (currentIndex < input.Length)
            textLength += input.Length - currentIndex;

        if (textLength <= maxLength)
            return input;

        return Truncate(input, maxLength, suffix);
    }

    #endregion

    #region 文本格式化功能

    /// <summary>
    /// 格式化文件大小
    /// </summary>
    /// <param name="bytes">字节数</param>
    /// <param name="decimalPlaces">小数位数</param>
    /// <returns>格式化后的文件大小字符串</returns>
    public static string FormatFileSize(long bytes, int decimalPlaces = 2)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB", "PB"];
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return string.Format("{{0:F" + decimalPlaces + "}} {1}", len, sizes[order]);
    }

    /// <summary>
    /// 格式化时间间隔
    /// </summary>
    /// <param name="timeSpan">时间间隔</param>
    /// <param name="showSeconds">是否显示秒数</param>
    /// <returns>格式化后的时间字符串</returns>
    public static string FormatTimeSpan(TimeSpan timeSpan, bool showSeconds = true)
    {
        if (timeSpan.TotalHours >= 1)
        {
            return showSeconds
                ? $"{(int)timeSpan.TotalHours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}"
                : $"{(int)timeSpan.TotalHours:D2}:{timeSpan.Minutes:D2}";
        }

        return showSeconds ? $"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}" : $"{timeSpan.Minutes:D2}";
    }

    /// <summary>
    /// 格式化数字，添加千位分隔符
    /// </summary>
    /// <param name="number">数字</param>
    /// <returns>格式化后的数字字符串</returns>
    public static string FormatNumber(long number)
    {
        return number.ToString("N0");
    }

    /// <summary>
    /// 格式化百分比
    /// </summary>
    /// <param name="value">值</param>
    /// <param name="total">总值</param>
    /// <param name="decimalPlaces">小数位数</param>
    /// <returns>格式化后的百分比字符串</returns>
    public static string FormatPercentage(double value, double total, int decimalPlaces = 1)
    {
        if (total == 0)
            return "0%";
        double percentage = (value / total) * 100;
        return string.Format("{{0:F" + decimalPlaces + "}}%", percentage);
    }

    #endregion

    #region 文本清理和验证功能

    /// <summary>
    /// 移除字符串中的特殊字符，只保留字母、数字和空格
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <returns>清理后的字符串</returns>
    public static string RemoveSpecialCharacters(string input)
    {
        return string.IsNullOrEmpty(input) ? input : MyRegex1().Replace(input, "");
    }

    /// <summary>
    /// 移除多余的空白字符
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <returns>清理后的字符串</returns>
    public static string RemoveExtraWhitespace(string input)
    {
        return string.IsNullOrEmpty(input) ? input : MyRegex2().Replace(input, " ").Trim();
    }

    /// <summary>
    /// 验证字符串是否为有效的邮箱地址
    /// </summary>
    /// <param name="email">邮箱地址</param>
    /// <returns>是否为有效邮箱</returns>
    public static bool IsValidEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
            return false;

        try
        {
            var regex = MyRegex3();
            return regex.IsMatch(email);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 验证字符串是否为有效的URL
    /// </summary>
    /// <param name="url">URL字符串</param>
    /// <returns>是否为有效URL</returns>
    public static bool IsValidUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
            return false;

        return Uri.TryCreate(url, UriKind.Absolute, out _);
    }

    #endregion

    #region 文本转换功能

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
        return string.IsNullOrEmpty(input) ? input : MyRegex4().Replace(input, "$1_$2").ToLowerInvariant();
    }

    /// <summary>
    /// 将字符串转换为短横线命名
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <returns>短横线命名字符串</returns>
    public static string ToKebabCase(string input)
    {
        return string.IsNullOrEmpty(input) ? input : MyRegex4().Replace(input, "$1-$2").ToLowerInvariant();
    }

    #endregion

    #region 文本统计功能

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

    #endregion

    [GeneratedRegex(@"<[^>]*>")]
    private static partial Regex MyRegex();

    [GeneratedRegex(@"[^a-zA-Z0-9\s]")]
    private static partial Regex MyRegex1();

    [GeneratedRegex(@"\s+")]
    private static partial Regex MyRegex2();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex MyRegex3();

    [GeneratedRegex(@"([a-z])([A-Z])")]
    private static partial Regex MyRegex4();
}
