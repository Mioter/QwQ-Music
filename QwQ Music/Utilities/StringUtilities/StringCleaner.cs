using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace QwQ_Music.Utilities.StringUtilities;

/// <summary>
/// 字符串清理工具类，提供各种字符串清理和纯文本化功能
/// </summary>
public static partial class StringCleaner
{
    /// <summary>
    /// 将字符串转换为纯文本（类似Word的粘贴为纯文本功能）
    /// 去除格式控制字符、不可见字符、特殊字符，仅保留可读文本
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <param name="preserveLineBreaks">是否保留换行符</param>
    /// <param name="preserveSpaces">是否保留空格</param>
    /// <returns>纯文本字符串</returns>
    public static string ToPlainText(string input, bool preserveLineBreaks = true, bool preserveSpaces = true)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var result = new StringBuilder();
        bool lastWasWhitespace = false;
        bool lastWasLineBreak = false;

        foreach (char c in input.Where(c => !char.IsControl(c) || c == '\n' || c == '\r' || c == '\t'))
        {
            // 处理换行符
            if (c is '\n' or '\r')
            {
                if (preserveLineBreaks && !lastWasLineBreak)
                {
                    result.Append('\n');
                    lastWasLineBreak = true;
                    lastWasWhitespace = false;
                }
                continue;
            }

            // 处理空格和制表符
            if (char.IsWhiteSpace(c))
            {
                if (preserveSpaces && !lastWasWhitespace)
                {
                    result.Append(' ');
                    lastWasWhitespace = true;
                }
                lastWasLineBreak = false;
                continue;
            }

            // 保留可打印字符
            if (!char.IsLetterOrDigit(c) && !char.IsPunctuation(c) && !char.IsSymbol(c))
                continue;

            result.Append(c);
            lastWasWhitespace = false;
            lastWasLineBreak = false;
        }

        return result.ToString().Trim();
    }

    /// <summary>
    /// 去除字符串中的所有格式控制字符和不可见字符
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <returns>清理后的字符串</returns>
    public static string RemoveControlCharacters(string input)
    {
        return string.IsNullOrEmpty(input)
            ? input
            : new string(input.Where(c => !char.IsControl(c) || c == '\n' || c == '\r' || c == '\t').ToArray());
    }

    /// <summary>
    /// 去除字符串中的零宽字符（零宽空格、零宽连字符等）
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <returns>清理后的字符串</returns>
    public static string RemoveZeroWidthCharacters(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // 零宽字符的Unicode范围
        return new string(
            input
                .Where(c =>
                        c != '\u200B'
                     && // 零宽空格
                        c != '\u200C'
                     && // 零宽非连接符
                        c != '\u200D'
                     && // 零宽连接符
                        c != '\u2060'
                     && // 词连接符
                        c != '\uFEFF'
                     && // 字节顺序标记
                        c != '\u200E'
                     && // 从左到右标记
                        c != '\u200F'
                     && // 从右到左标记
                        c != '\u202A'
                     && // 从左到右嵌入
                        c != '\u202B'
                     && // 从右到左嵌入
                        c != '\u202C'
                     && // 弹出方向格式
                        c != '\u202D'
                     && // 从左到右覆盖
                        c != '\u202E' // 从右到左覆盖
                )
                .ToArray()
        );
    }

    /// <summary>
    /// 合并重复的空白字符（空格、制表符、换行符）
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <param name="normalizeLineBreaks">是否统一换行符为\n</param>
    /// <returns>清理后的字符串</returns>
    public static string NormalizeWhitespace(string input, bool normalizeLineBreaks = true)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        string result = input;

        // 统一换行符
        if (normalizeLineBreaks)
        {
            result = result.Replace("\r\n", "\n").Replace("\r", "\n");
        }

        // 合并连续的空白字符
        result = WhitespaceRegex().Replace(result, " ");

        // 合并连续的换行符
        result = LineBreakRegex().Replace(result, "\n\n");

        return result.Trim();
    }

    /// <summary>
    /// 去除字符串中的HTML标签
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <returns>去除HTML标签后的字符串</returns>
    public static string RemoveHtmlTags(string input)
    {
        return string.IsNullOrEmpty(input) ? input : HtmlTagRegex().Replace(input, "");
    }

    /// <summary>
    /// 去除字符串中的Markdown标记
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <returns>去除Markdown标记后的字符串</returns>
    public static string RemoveMarkdown(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // 去除标题标记
        input = HeadingRegex().Replace(input, "");

        // 去除粗体和斜体标记
        input = BoldItalicRegex().Replace(input, "$1");

        // 去除代码标记
        input = CodeRegex().Replace(input, "$1");

        // 去除链接标记
        input = LinkRegex().Replace(input, "$1");

        // 去除图片标记
        input = ImageRegex().Replace(input, "$1");

        return input;
    }

    /// <summary>
    /// 清理字符串，去除所有非ASCII字符
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <returns>仅包含ASCII字符的字符串</returns>
    public static string ToAsciiOnly(string input)
    {
        return string.IsNullOrEmpty(input) ? input : new string(input.Where(c => c <= 127).ToArray());
    }

    /// <summary>
    /// 清理字符串，去除所有非字母数字字符（替代RemoveSpecialCharacters）
    /// 只保留字母、数字，可选择是否保留空格
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <param name="preserveSpaces">是否保留空格</param>
    /// <returns>仅包含字母数字的字符串</returns>
    public static string ToAlphanumericOnly(string input, bool preserveSpaces = false)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return preserveSpaces
            ? new string(input.Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)).ToArray())
            : new string(input.Where(char.IsLetterOrDigit).ToArray());
    }

    /// <summary>
    /// 移除多余的空白字符
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <returns>清理后的字符串</returns>
    public static string RemoveExtraWhitespace(string input)
    {
        return string.IsNullOrEmpty(input) ? input : WhitespaceRegex().Replace(input, " ").Trim();
    }

    /// <summary>
    /// 综合清理字符串，应用多种清理方法
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <param name="removeHtml">是否去除HTML标签</param>
    /// <param name="removeMarkdown">是否去除Markdown标记</param>
    /// <param name="removeZeroWidth">是否去除零宽字符</param>
    /// <param name="normalizeWhitespace">是否标准化空白字符</param>
    /// <returns>综合清理后的字符串</returns>
    public static string CleanComprehensive(string input, bool removeHtml = true, bool removeMarkdown = true, 
        bool removeZeroWidth = true, bool normalizeWhitespace = true)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        string result = input;

        if (removeZeroWidth)
            result = RemoveZeroWidthCharacters(result);

        if (removeHtml)
            result = RemoveHtmlTags(result);

        if (removeMarkdown)
            result = RemoveMarkdown(result);

        if (normalizeWhitespace)
            result = NormalizeWhitespace(result);

        return result;
    }

    #region 正则表达式

    [GeneratedRegex(@"<[^>]*>")]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"\n+")]
    private static partial Regex LineBreakRegex();

    [GeneratedRegex(@"^#{1,6}\s+")]
    private static partial Regex HeadingRegex();

    [GeneratedRegex(@"\*\*([^*]+)\*\*")]
    private static partial Regex BoldItalicRegex();

    [GeneratedRegex(@"`([^`]+)`")]
    private static partial Regex CodeRegex();

    [GeneratedRegex(@"\[([^\]]+)\]\([^)]+\)")]
    private static partial Regex LinkRegex();

    [GeneratedRegex(@"!\[([^\]]+)\]\([^)]+\)")]
    private static partial Regex ImageRegex();

    #endregion
}