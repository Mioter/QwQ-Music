using System.Text.RegularExpressions;

namespace QwQ_Music.Utilities.StringUtilities;

/// <summary>
/// 字符串裁剪工具类
/// </summary>
public static partial class StringTruncator
{
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
        var regex = HtmlTagRegex();
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

    [GeneratedRegex(@"<[^>]*>")]
    private static partial Regex HtmlTagRegex();
}
