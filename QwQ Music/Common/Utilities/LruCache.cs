using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace QwQ_Music.Common.Utilities;

/// <summary>
///     最近最少使用（LRU）缓存实现
/// </summary>
/// <typeparam name="TKey">缓存键类型</typeparam>
/// <typeparam name="TValue">缓存值类型</typeparam>
public class LruCache<TKey, TValue>
    where TKey : notnull
{
    private readonly Dictionary<TKey, CacheEntry> _cache = new();
    private readonly Lock _lock = new();
    private readonly Action<TValue>? _onItemRemoved;

    /// <summary>
    ///     初始化 LRU 缓存
    /// </summary>
    /// <param name="maxCacheSize">最大缓存数量，默认为50</param>
    /// <param name="onItemRemoved">当缓存项被移除时的回调函数，用于清理资源</param>
    public LruCache(int maxCacheSize = 50, Action<TValue>? onItemRemoved = null)
    {
        MaxCacheSize = maxCacheSize;
        _onItemRemoved = onItemRemoved;
    }

    /// <summary>
    ///     获取或设置最大缓存数量
    /// </summary>
    public int MaxCacheSize { get; set; }

    /// <summary>
    ///     获取或设置缓存项
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <exception cref="KeyNotFoundException">当键不存在时抛出</exception>
    public TValue this[TKey key]
    {
        get
        {
            lock (_lock)
            {
                if (_cache.TryGetValue(key, out var entry))
                {
                    UpdateAccessTime(key, entry);

                    return entry.Value;
                }

                throw new KeyNotFoundException($"The key '{key}' was not found in the cache.");
            }
        }
        set
        {
            lock (_lock)
            {
                EnsureCapacity();
                _cache[key] = new CacheEntry(value, DateTime.Now);
            }
        }
    }

    /// <summary>
    ///     获取当前缓存数量
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _cache.Count;
            }
        }
    }

    /// <summary>
    ///     尝试获取缓存值
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="value">缓存值</param>
    /// <returns>是否成功获取</returns>
    public bool TryGetValue(TKey key, out TValue? value)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var entry))
            {
                UpdateAccessTime(key, entry);
                value = entry.Value;

                return true;
            }

            value = default;

            return false;
        }
    }

    /// <summary>
    ///     添加缓存项
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="value">缓存值</param>
    public void Add(TKey key, TValue value)
    {
        lock (_lock)
        {
            EnsureCapacity();
            _cache[key] = new CacheEntry(value, DateTime.Now);
        }
    }

    /// <summary>
    ///     移除缓存项
    /// </summary>
    /// <param name="key">缓存键</param>
    public void Remove(TKey key)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var entry))
            {
                _onItemRemoved?.Invoke(entry.Value);
                _cache.Remove(key);
            }
        }
    }

    /// <summary>
    ///     清空缓存
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            if (_onItemRemoved != null)
            {
                foreach (var entry in _cache.Values)
                {
                    _onItemRemoved(entry.Value);
                }
            }

            _cache.Clear();
        }
    }

    /// <summary>
    ///     检查键是否存在
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <returns>是否存在</returns>
    public bool ContainsKey(TKey key)
    {
        lock (_lock)
        {
            return _cache.ContainsKey(key);
        }
    }

    private void EnsureCapacity()
    {
        if (_cache.Count >= MaxCacheSize)
        {
            var oldestKey = _cache.OrderBy(x => x.Value.LastAccess).First().Key;

            if (_cache.TryGetValue(oldestKey, out var entry))
            {
                _onItemRemoved?.Invoke(entry.Value);
            }

            _cache.Remove(oldestKey);
        }
    }

    private void UpdateAccessTime(TKey key, CacheEntry entry)
    {
        _cache[key] = entry with
        {
            LastAccess = DateTime.Now,
        };
    }

    private readonly record struct CacheEntry(TValue Value, DateTime LastAccess);
}
