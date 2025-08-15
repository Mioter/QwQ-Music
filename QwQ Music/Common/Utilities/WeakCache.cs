using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace QwQ_Music.Common.Utilities;

/// <summary>
///     使用弱引用的通用缓存实现，支持部分清理
/// </summary>
/// <remarks>
///     构造函数
/// </remarks>
/// <param name="cleanupBatchSize">每次清理时检查的项目数</param>
public class WeakCache<TKey, TValue>(int cleanupBatchSize = 10)
    where TValue : class
    where TKey : notnull
{
    private readonly Dictionary<TKey, (WeakReference<TValue> Reference, DateTime LastAccess)> _cache = new();
    private readonly Lock _lock = new();

    /// <summary>
    ///     获取或设置缓存项
    /// </summary>
    public TValue this[TKey key]
    {
        get
        {
            lock (_lock)
            {
                if (_cache.TryGetValue(key, out var tuple) && tuple.Reference.TryGetTarget(out var value))
                {
                    _cache[key] = (tuple.Reference, DateTime.UtcNow); // 更新访问时间

                    return value;
                }

                throw new KeyNotFoundException($"The key '{key}' was not found in the cache.");
            }
        }
        set
        {
            lock (_lock)
            {
                CleanupDeadReferences();
                _cache[key] = (new WeakReference<TValue>(value), DateTime.UtcNow);
            }
        }
    }

    /// <summary>
    ///     获取当前有效缓存数量
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                CleanupDeadReferences();

                return _cache.Count(kvp => kvp.Value.Reference.TryGetTarget(out _));
            }
        }
    }

    /// <summary>
    ///     尝试获取缓存值
    /// </summary>
    public bool TryGetValue(TKey key, out TValue? value)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var tuple) && tuple.Reference.TryGetTarget(out var v))
            {
                _cache[key] = (tuple.Reference, DateTime.UtcNow); // 更新访问时间
                value = v;

                return true;
            }

            value = null;

            return false;
        }
    }

    /// <summary>
    ///     添加缓存项
    /// </summary>
    public void Add(TKey key, TValue value)
    {
        lock (_lock)
        {
            CleanupDeadReferences();
            _cache[key] = (new WeakReference<TValue>(value), DateTime.UtcNow);
        }
    }

    /// <summary>
    ///     移除缓存项
    /// </summary>
    public void Remove(TKey key)
    {
        lock (_lock)
        {
            _cache.Remove(key);
        }
    }

    /// <summary>
    ///     清空缓存
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _cache.Clear();
        }
    }

    /// <summary>
    ///     检查键是否存在
    /// </summary>
    public bool ContainsKey(TKey key)
    {
        lock (_lock)
        {
            return _cache.TryGetValue(key, out var tuple) && tuple.Reference.TryGetTarget(out _);
        }
    }

    /// <summary>
    ///     部分清理已失效的弱引用（只检查最久未访问的batchSize个项目）
    /// </summary>
    private void CleanupDeadReferences()
    {
        if (_cache.Count == 0)
            return;

        if (cleanupBatchSize <= 0)
        {
            // 清理全部失效引用
            var deadKeys = _cache
                .Where(kvp => !kvp.Value.Reference.TryGetTarget(out _))
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in deadKeys)
            {
                _cache.Remove(key);
            }
        }
        else
        {
            // 取最久未访问的batchSize个key
            var oldest = _cache.OrderBy(kvp => kvp.Value.LastAccess).Take(cleanupBatchSize).ToList();

            foreach (var kvp in oldest.Where(kvp => !kvp.Value.Reference.TryGetTarget(out _)))
            {
                _cache.Remove(kvp.Key);
            }
        }
    }
}
