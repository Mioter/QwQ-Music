using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using Avalonia.Media.Imaging;
using QwQ_Music.Common.Managers;
using QwQ_Music.Common.Services;

namespace QwQ_Music.Common.Helpers;

public static class ParseHelper {
    [Pure]
    public static TimeSpan TimeSpanParser(string timeStamp) {
        // 1-> 00:00:01.000
        // 1:2.03-> 00:01:02.030
        // 1:2:3.4567899 -> 1:02:03.4567899
        // 1.2:3:4.5678 -> 1.02:03:04.5678000
        // [-]d.hh:mm:ss.fffffff
        int negative = timeStamp.IndexOf('-');
        string[] times = timeStamp[(negative + 1)..].Split(':');
        int d = 0, h = 0, m = 0, s, ms, us;
        switch (times.Length) {
            case 1:
                // No colon => second.milliseconds
                (s, ms, us) = _parse_ss(times[0]);
                break;
            case 2:
                // Only 1 colon => mm:ss[.fffffff]
                (s, ms, us) = _parse_ss(times[1]);
                m = int.Parse(times[0]);
                break;
            case 3:
                // Both colons => [d.]hh:mm:ss[.fffffff]
                (d, h) = _parse_dh(times[0]);
                m = int.Parse(times[1]);
                (s, ms, us) = _parse_ss(times[2]);
                break;
            default:
                throw new FormatException($"{timeStamp} is not a valid TimeSpan format.");
        }

        return new TimeSpan(d, h, m, s, ms, us);

        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // ReSharper disable once InconsistentNaming
        static (int Second, int Millisecond, int Microsecond) _parse_ss(string time) {
            string[] sms = time.Split('.');
            if (sms.Length == 1)
                return (int.Parse(sms[0]), 0, 0);
            int _1s = int.Parse(sms[1]);
            return (int.Parse(sms[0]), _1s / 1000, _1s % 1000);
        }

        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // ReSharper disable once InconsistentNaming
        static (int Day, int Hour) _parse_dh(string time) {
            string[] sms = time.Split('.');
            if (sms.Length == 1)
                return (0, int.Parse(sms[0]));

            return (int.Parse(sms[0]), int.Parse(sms[1]));
        }
    }

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T? TryParse<T>(Dictionary<string, object?> dict, string key) where T : struct, IParsable<T> {
        if (!dict.TryGetValue(key, out object? value) || value is null)
            return null;
        if (value is T rst)
            return rst;
        T.TryParse(value.ToString(), null, out T result);
        return result;
    }

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [return: NotNullIfNotNull(nameof(notnull))]
    public static string? TryParse(Dictionary<string, object?> dict, string key, bool? notnull = null) {
        if (dict.TryGetValue(key, out object? value) && value is string valueString)
            return valueString;
        if (notnull is not null)
            throw new NullReferenceException();
        return null;
    }

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueTuple<string, string>? TryParseTuple(
        Dictionary<string, object?> dict,
        string key,
        params char[] separator) {
        if (!dict.TryGetValue(key, out object? value) || value is not string str)
            return null;
        string[] data = str.Trim().Split(separator, StringSplitOptions.RemoveEmptyEntries);
        return data.Length == 0 ? ("", "") : (data[0], data[1]);
    }

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Bitmap? ParseToBitmap(object? storage) {
        if (storage is not byte[] data)
            return null;
        try {
            using var dataStream = new MemoryStream(data);
            return new Bitmap(dataStream);
        } catch (Exception ex) {
            LoggerService.Error("从数据库中读取封面缩略图失败", ex);
            return null;
        }
    }
}

public static class DumpHelper {
    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [return: NotNullIfNotNull(nameof(bitmap))]
    public static byte[]? BitmapToBytes(Bitmap? bitmap) {
        if (bitmap is null || !CacheManager.IsValid(bitmap))
            return null;
        var stream = new MemoryStream();
        bitmap.Save(stream);
        return stream.ToArray();
    }
}