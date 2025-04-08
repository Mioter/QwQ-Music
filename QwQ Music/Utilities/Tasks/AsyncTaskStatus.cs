namespace QwQ_Music.Utilities.Tasks;

/// <summary>
/// 异步任务状态枚举
/// </summary>
public enum AsyncTaskStatus
{
    /// <summary>
    /// 任务已创建但尚未开始
    /// </summary>
    Created,

    /// <summary>
    /// 任务正在运行
    /// </summary>
    Running,

    /// <summary>
    /// 任务已暂停
    /// </summary>
    Paused,

    /// <summary>
    /// 任务已完成
    /// </summary>
    Completed,

    /// <summary>
    /// 任务已取消
    /// </summary>
    Cancelled,

    /// <summary>
    /// 任务执行出错
    /// </summary>
    Faulted,
}
