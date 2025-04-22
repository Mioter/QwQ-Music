using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace QwQ_Music.Utilities.MessageBus;

/// <summary>
///     订阅条目，存储特定消息类型的所有订阅
/// </summary>
public class SubscriptionEntry
{
    private readonly List<object> _subscriptions = new();
    private readonly ReaderWriterLockSlim _lock = new();

    /// <summary>
    ///     添加订阅
    /// </summary>
    /// <param name="subscription">订阅对象</param>
    public void AddSubscription(object subscription)
    {
        _lock.EnterWriteLock();
        try
        {
            _subscriptions.Add(subscription);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    ///     移除订阅
    /// </summary>
    /// <param name="subscription">订阅对象</param>
    /// <returns>是否成功移除</returns>
    public bool RemoveSubscription(object subscription)
    {
        _lock.EnterWriteLock();
        try
        {
            return _subscriptions.Remove(subscription);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    ///     获取订阅快照
    /// </summary>
    /// <returns>订阅对象列表</returns>
    public List<object> GetSubscriptionsSnapshot()
    {
        _lock.EnterReadLock();
        try
        {
            return _subscriptions.ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    ///     获取订阅数量
    /// </summary>
    public int Count
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                return _subscriptions.Count;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }
}
