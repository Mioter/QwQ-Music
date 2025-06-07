using System.Threading;

namespace QwQ_Music.Services;

/// <summary>
/// 提供实例级别的唯一 ID 生成器（每个实例有自己的 ID 序列）
/// </summary>
public class InstanceIdGenerator
{
    private long _currentId;

    /// <summary>
    /// 获取下一个唯一 ID
    /// </summary>
    public long GetNextId()
    {
        return Interlocked.Increment(ref _currentId);
    }

    /// <summary>
    /// 获取当前实例的当前 ID 值（只读）
    /// </summary>
    public long CurrentId => _currentId;
}

/// <summary>
/// 全局静态 ID 生成器（保留原有功能）
/// </summary>
public static class UniqueIdGenerator
{
    private static long currentId;

    /// <summary>
    /// 获取全局唯一的 ID
    /// </summary>
    public static long GetNextId()
    {
        return Interlocked.Increment(ref currentId);
    }
}
