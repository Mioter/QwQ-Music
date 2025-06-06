using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Avalonia.Media.Imaging;

namespace QwQ_Music.Utilities;

/// <summary>
/// 使用弱引用的图片缓存实现
/// </summary>
public class WeakImageCache
{
    private readonly Dictionary<string, WeakReference<Bitmap>> _cache = new();
    private readonly Lock _lock = new();

    /// <summary>
    /// 获取或设置缓存项
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <exception cref="KeyNotFoundException">当键不存在时抛出</exception>
    public Bitmap this[string key]
    {
        get
        {
            lock (_lock)
            {
                if (_cache.TryGetValue(key, out var weakRef) && weakRef.TryGetTarget(out var bitmap))
                {
                    return bitmap;
                }
                throw new KeyNotFoundException($"The key '{key}' was not found in the cache.");
            }
        }
        set
        {
            lock (_lock)
            {
                CleanupDeadReferences();
                _cache[key] = new WeakReference<Bitmap>(value);
            }
        }
    }

    /// <summary>
    /// 尝试获取缓存值
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="value">缓存值</param>
    /// <returns>是否成功获取</returns>
    public bool TryGetValue(string key, out Bitmap? value)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var weakRef) && weakRef.TryGetTarget(out var bitmap))
            {
                value = bitmap;
                return true;
            }
            value = null;
            return false;
        }
    }

    /// <summary>
    /// 添加缓存项
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="value">缓存值</param>
    public void Add(string key, Bitmap value)
    {
        lock (_lock)
        {
            CleanupDeadReferences();
            _cache[key] = new WeakReference<Bitmap>(value);
        }
    }

    /// <summary>
    /// 移除缓存项
    /// </summary>
    /// <param name="key">缓存键</param>
    public void Remove(string key)
    {
        lock (_lock)
        {
            _cache.Remove(key);
        }
    }

    /// <summary>
    /// 清空缓存
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _cache.Clear();
        }
    }

    /// <summary>
    /// 检查键是否存在
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <returns>是否存在</returns>
    public bool ContainsKey(string key)
    {
        lock (_lock)
        {
            return _cache.TryGetValue(key, out var weakRef) && weakRef.TryGetTarget(out _);
        }
    }

    /// <summary>
    /// 获取当前缓存数量
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                CleanupDeadReferences();
                return _cache.Count;
            }
        }
    }

    /// <summary>
    /// 清理已失效的弱引用
    /// </summary>
    private void CleanupDeadReferences()
    {
        var deadKeys = _cache.Where(kvp => !kvp.Value.TryGetTarget(out _)).Select(kvp => kvp.Key).ToList();

        foreach (string key in deadKeys)
        {
            _cache.Remove(key);
        }
    }
}
