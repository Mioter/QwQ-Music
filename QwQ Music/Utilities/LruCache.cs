using System;
using System.Collections.Generic;
using System.Linq;

namespace QwQ_Music.Utilities;

/// <summary>
/// 基于LRU (Least Recently Used) 算法的缓存字典
/// 当缓存项数量超过最大容量时，会自动移除最久未使用的项
/// </summary>
/// <typeparam name="TKey">键的类型</typeparam>
/// <typeparam name="TValue">值的类型</typeparam>
public class LruCache<TKey, TValue> where TKey : notnull
{
    private readonly int _capacity;
    private readonly Dictionary<TKey, LinkedListNode<CacheItem>> _cacheMap;
    private readonly LinkedList<CacheItem> _lruList;
    private readonly Action<TValue>? _disposeAction;

    /// <summary>
    /// 缓存项，包含键和值
    /// </summary>
    private class CacheItem(TKey key, TValue value)
    {
        public TKey Key { get; } = key;
        public TValue Value { get; } = value;

    }

    /// <summary>
    /// 获取缓存中的项数
    /// </summary>
    public int Count => _cacheMap.Count;

    /// <summary>
    /// 获取缓存的最大容量
    /// </summary>
    public int Capacity => _capacity;

    /// <summary>
    /// 创建一个新的LRU缓存
    /// </summary>
    /// <param name="capacity">缓存的最大容量，默认为100</param>
    /// <param name="disposeAction">当项被移除时的资源释放行为</param>
    public LruCache(int capacity = 100, Action<TValue>? disposeAction = null)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), "缓存容量必须大于0");

        _capacity = capacity;
        _cacheMap = new Dictionary<TKey, LinkedListNode<CacheItem>>(capacity);
        _lruList = [];
        _disposeAction = disposeAction;
    }

    /// <summary>
    /// 获取或设置指定键的值
    /// </summary>
    /// <param name="key">键</param>
    /// <returns>值，如果键不存在则抛出异常</returns>
    public TValue this[TKey key]
    {
        get
        {
            if (_cacheMap.TryGetValue(key, out var node))
            {
                // 将访问的节点移到链表头部，表示最近使用
                _lruList.Remove(node);
                _lruList.AddFirst(node);
                return node.Value.Value;
            }

            throw new KeyNotFoundException($"键 '{key}' 在缓存中不存在");
        }
        set => AddOrUpdate(key, value);
    }

    /// <summary>
    /// 尝试获取指定键的值
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="value">如果找到键，则包含键的值；否则为默认值</param>
    /// <returns>如果缓存包含具有指定键的元素，则为true；否则为false</returns>
    public bool TryGetValue(TKey key, out TValue value)
    {
        if (_cacheMap.TryGetValue(key, out var node))
        {
            // 将访问的节点移到链表头部，表示最近使用
            _lruList.Remove(node);
            _lruList.AddFirst(node);
            value = node.Value.Value;
            return true;
        }

        value = default!;
        return false;
    }

    /// <summary>
    /// 添加或更新缓存中的项
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    public void AddOrUpdate(TKey key, TValue value)
    {
        if (_cacheMap.TryGetValue(key, out var existingNode))
        {
            // 如果键已存在，移除旧节点
            _lruList.Remove(existingNode);
            _cacheMap.Remove(key);
            
            // 如果有释放行为，释放旧值
            if (_disposeAction != null)
            {
                _disposeAction(existingNode.Value.Value);
            }
        }
        else if (_cacheMap.Count >= _capacity)
        {
            // 如果达到容量上限，移除最久未使用的项（链表尾部）
            RemoveOldest();
        }

        // 创建新节点并添加到链表头部和字典中
        var cacheItem = new CacheItem(key, value);
        var newNode = _lruList.AddFirst(cacheItem);
        _cacheMap[key] = newNode;
    }

    /// <summary>
    /// 从缓存中移除指定键的项
    /// </summary>
    /// <param name="key">要移除的键</param>
    /// <returns>如果成功移除项，则为true；否则为false</returns>
    public bool Remove(TKey key)
    {
        if (_cacheMap.TryGetValue(key, out var node))
        {
            // 从链表和字典中移除
            _lruList.Remove(node);
            _cacheMap.Remove(key);
            
            // 如果有释放行为，释放值
            if (_disposeAction != null)
            {
                _disposeAction(node.Value.Value);
            }
            
            return true;
        }
        
        return false;
    }

    /// <summary>
    /// 清空缓存
    /// </summary>
    public void Clear()
    {
        // 如果有释放行为，释放所有值
        if (_disposeAction != null)
        {
            foreach (var node in _lruList)
            {
                _disposeAction(node.Value);
            }
        }
        
        _lruList.Clear();
        _cacheMap.Clear();
    }

    /// <summary>
    /// 检查缓存是否包含指定的键
    /// </summary>
    /// <param name="key">要检查的键</param>
    /// <returns>如果缓存包含指定的键，则为true；否则为false</returns>
    public bool ContainsKey(TKey key) => _cacheMap.ContainsKey(key);

    /// <summary>
    /// 获取缓存中所有键的集合
    /// </summary>
    public IEnumerable<TKey> Keys => _cacheMap.Keys;

    /// <summary>
    /// 获取缓存中所有值的集合
    /// </summary>
    public IEnumerable<TValue> Values => _lruList.Select(item => item.Value);

    /// <summary>
    /// 移除最久未使用的项（链表尾部的项）
    /// </summary>
    private void RemoveOldest()
    {
        var oldest = _lruList.Last;
        if (oldest != null)
        {
            _lruList.RemoveLast();
            _cacheMap.Remove(oldest.Value.Key);
            
            // 如果有释放行为，释放值
            if (_disposeAction != null)
            {
                _disposeAction(oldest.Value.Value);
            }
        }
    }
}