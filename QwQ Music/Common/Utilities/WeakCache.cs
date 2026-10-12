using System.Collections.Concurrent;

namespace QwQ_Music.Common.Utilities;

/// <summary>
///     使用弱引用的通用缓存实现，支持部分清理
/// </summary>
/// <remarks>
///     构造函数
/// </remarks>
/// <param name="cleanupBatchSize">每次清理时检查的项目数</param>
public sealed class WeakCache<TKey, TValue>(int cleanupBatchSize = 20, int cleanInterval = 15)
    : IDisposable where TValue : class
                  where TKey : notnull {
    private int _cleanCount;
    private readonly ConcurrentDictionary<TKey, (WeakReference<TValue> Reference, DateTime LastAccess)> _cache = new();

    /// <summary>
    ///     获取或设置缓存项
    /// </summary>
    public TValue this[TKey key] {
        get {
            if (_cache.TryGetValue(key, out (WeakReference<TValue> Reference, DateTime LastAccess) tuple) &&
                tuple.Reference.TryGetTarget(out TValue? value)) {
                _cache[key] = (tuple.Reference, DateTime.UtcNow); // 更新访问时间

                return value;
            }

            throw new KeyNotFoundException($"The key '{key}' was not found in the cache.");
        }
        set {
            CleanZombies();
            _cache[key] = (new WeakReference<TValue>(value), DateTime.UtcNow);
        }
    }

    /// <summary>
    ///     获取当前有效缓存数量
    /// </summary>
    public int Count {
        get {
            CleanZombies(true);

            return _cache.Count;
        }
    }

    /// <summary>
    ///     尝试获取缓存值
    /// </summary>
    public bool TryGetValue(TKey key, out TValue? value) {
        if (_cache.TryGetValue(key, out (WeakReference<TValue> Reference, DateTime LastAccess) tuple) &&
            tuple.Reference.TryGetTarget(out TValue? v)) {
            _cache[key] = (tuple.Reference, DateTime.UtcNow); // 更新访问时间
            value = v;

            return true;
        }

        value = null;

        return false;
    }

    /// <summary>
    ///     添加缓存项
    /// </summary>
    public void Add(TKey key, TValue value) {
        CleanZombies();
        _cache[key] = (new WeakReference<TValue>(value), DateTime.UtcNow);
    }

    /// <summary>
    ///     移除缓存项
    /// </summary>
    public void Remove(TKey key) { _cache.Remove(key, out _); }

    /// <summary>
    ///     清空缓存
    /// </summary>
    public void Clear() { _cache.Clear(); }

    /// <summary>
    ///     检查键是否存在
    /// </summary>
    public bool ContainsKey(TKey key) {
        return _cache.TryGetValue(key, out (WeakReference<TValue> Reference, DateTime LastAccess) tuple) &&
               tuple.Reference.TryGetTarget(out _);
    }

    /// <summary>
    ///     部分清理已失效的弱引用（只检查最久未访问的batchSize个项目）
    /// </summary>
    private void CleanZombies(bool isForced = false) {
        if (_cache.IsEmpty)
            return;
        if (!isForced && Interlocked.Increment(ref _cleanCount) <= cleanInterval)
            return;
        Interlocked.Exchange(ref _cleanCount, 0);
        if (isForced || cleanupBatchSize <= 0) {
            // 清理全部失效引用
            IEnumerable<TKey> deadKeys =
                _cache.Where(kvp => !kvp.Value.Reference.TryGetTarget(out _)).Select(kvp => kvp.Key);

            foreach (TKey key in deadKeys)
                _cache.Remove(key, out _);
        } else {
            // 取最久未访问的batchSize个key
            IEnumerable<KeyValuePair<TKey, (WeakReference<TValue> Reference, DateTime LastAccess)>> oldest =
                _cache.OrderBy(kvp => kvp.Value.LastAccess).Take(cleanupBatchSize);

            foreach (KeyValuePair<TKey, (WeakReference<TValue> Reference, DateTime LastAccess)> kvp in
                     oldest.Where(kvp => !kvp.Value.Reference.TryGetTarget(out _)))
                _cache.Remove(kvp.Key, out _);
        }
    }

    public void Dispose() {
        if (typeof(TKey).IsAssignableTo(typeof(IDisposable)) && typeof(TValue).IsAssignableTo(typeof(IDisposable))) {
            _cache.AsParallel()
                  .ForAll(item => {
                      ((IDisposable?)item.Key)?.Dispose();
                      if (item.Value.Reference.TryGetTarget(out TValue? value))
                          ((IDisposable?)value)?.Dispose();
                  });
        } else if (typeof(TKey).IsAssignableTo(typeof(IDisposable))) {
            _cache.Keys.AsParallel().ForAll(key => { ((IDisposable)key).Dispose(); });
        } else if (typeof(TValue).IsAssignableTo(typeof(IDisposable))) {
            _cache.Values.AsParallel()
                  .Select(item => item.Reference)
                  .ForAll(item => {
                      if (item.TryGetTarget(out TValue? value))
                          ((IDisposable?)value)?.Dispose();
                  });
        }

        _cache.Clear();
    }
}