using System;
using System.Text.RegularExpressions;

namespace QwQ_Music.Utilities.StringUtilities;

/// <summary>
/// 字符串验证工具类
/// </summary>
public static partial class StringValidator
{
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
            var regex = EmailRegex();
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
        return !string.IsNullOrEmpty(url) && Uri.TryCreate(url, UriKind.Absolute, out _);
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();
}
