using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Avalonia.Media;
using QwQ_Music.Utilities;

namespace QwQ_Music.Services;

/// <summary>
/// 提供图标资源的服务类，支持缓存和错误处理
/// </summary>
public static class IconService
{
    private static readonly ConcurrentDictionary<string, StreamGeometry> _iconCache = new();

    /// <summary>
    /// 获取指定键的图标资源
    /// </summary>
    /// <param name="key">图标资源的键名</param>
    /// <returns>图标资源的 StreamGeometry 对象</returns>
    /// <exception cref="ArgumentNullException">当 key 为 null 或空字符串时抛出</exception>
    /// <exception cref="InvalidOperationException">当 Application.Current 为 null 时抛出</exception>
    /// <exception cref="KeyNotFoundException">当找不到指定的资源时抛出</exception>
    public static StreamGeometry GetIcon(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));

        // 尝试从缓存中获取
        if (_iconCache.TryGetValue(key, out var cachedIcon))
        {
            return cachedIcon;
        }

        try
        {
            var icon =
                ResourceDictionaryManager.Get<StreamGeometry>(key)
                ?? throw new KeyNotFoundException($"未找到键为 '{key}' 的图标资源");

            // 将图标添加到缓存
            _iconCache.TryAdd(key, icon);

            return icon;
        }
        catch (Exception ex)
            when (ex is not ArgumentException and not InvalidOperationException and not KeyNotFoundException)
        {
            LoggerService.Error($"获取图标资源时发生错误: \n 键名: {key} \n 错误信息: {ex.Message}");
            throw new InvalidOperationException($"获取图标资源时发生错误: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 清除图标缓存
    /// </summary>
    public static void ClearCache()
    {
        _iconCache.Clear();
        LoggerService.Info("图标缓存已清除");
    }

    /// <summary>
    /// 从缓存中移除指定的图标
    /// </summary>
    /// <param name="key">要移除的图标键名</param>
    /// <returns>是否成功移除</returns>
    public static bool RemoveFromCache(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));
        return _iconCache.TryRemove(key, out _);
    }
}
