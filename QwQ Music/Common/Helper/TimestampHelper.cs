using System;

namespace QwQ_Music.Common.Helper;

public static class TimestampHelper
{
    private static readonly DateTime _unixEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    ///     获取当前时间戳（秒）
    /// </summary>
    public static long GetTimestamp()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    /// <summary>
    ///     获取当前时间戳（毫秒）
    /// </summary>
    public static long GetTimestampMs()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    /// <summary>
    ///     DateTime转时间戳（秒）
    /// </summary>
    public static long ToTimestamp(this DateTime dateTime)
    {
        return ((DateTimeOffset)dateTime.ToUniversalTime()).ToUnixTimeSeconds();
    }

    /// <summary>
    ///     DateTime转时间戳（毫秒）
    /// </summary>
    public static long ToTimestampMs(this DateTime dateTime)
    {
        return ((DateTimeOffset)dateTime.ToUniversalTime()).ToUnixTimeMilliseconds();
    }

    /// <summary>
    ///     时间戳转DateTime（秒）
    /// </summary>
    public static DateTime ToDateTime(long timestamp)
    {
        return DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
    }

    /// <summary>
    ///     时间戳转DateTime（毫秒）
    /// </summary>
    public static DateTime ToDateTimeMs(long timestampMs)
    {
        return DateTimeOffset.FromUnixTimeMilliseconds(timestampMs).DateTime;
    }

    /// <summary>
    ///     将 DateTime 转换为 Unix 时间戳（秒）
    /// </summary>
    public static long ToUnixTimestamp(this DateTime dateTime)
    {
        return (long)(dateTime.ToUniversalTime() - _unixEpoch).TotalSeconds;
    }

    /// <summary>
    ///     将 DateTime 转换为 Unix 时间戳（毫秒）
    /// </summary>
    public static long ToUnixTimestampMilliseconds(this DateTime dateTime)
    {
        return (long)(dateTime.ToUniversalTime() - _unixEpoch).TotalMilliseconds;
    }

    /// <summary>
    ///     从 Unix 时间戳（秒）转换为 DateTime
    /// </summary>
    public static DateTime FromUnixTimestamp(long timestamp)
    {
        return _unixEpoch.AddSeconds(timestamp).ToLocalTime();
    }

    /// <summary>
    ///     从 Unix 时间戳（毫秒）转换为 DateTime
    /// </summary>
    public static DateTime FromUnixTimestampMilliseconds(long timestamp)
    {
        return _unixEpoch.AddMilliseconds(timestamp).ToLocalTime();
    }
}
