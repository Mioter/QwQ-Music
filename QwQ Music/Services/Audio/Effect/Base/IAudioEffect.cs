using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NAudio.Wave;

namespace QwQ_Music.Services.Audio.Effect.Base;

/// <summary>
/// 音频效果器统一接口，定义音频处理节点的基本行为
/// </summary>
public interface IAudioEffect : ISampleProvider
{
    /// <summary>
    /// 获取效果器的唯一标识名称
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 获取或设置效果器的启用状态（默认true）
    /// </summary>
    bool Enabled { get; set; }

    /// <summary>
    /// 获取或设置效果器执行优先级（数值越小越先执行）
    /// </summary>
    int Priority { get; set; }

    /// <summary>
    /// 同步初始化效果器并设置音频源
    /// </summary>
    /// <param name="source">音频源提供者</param>
    /// <exception cref="ArgumentNullException">当source为null时抛出</exception>
    void Initialize(ISampleProvider source);

    /// <summary>
    /// 异步初始化效果器并设置音频源
    /// </summary>
    /// <param name="source">音频源提供者</param>
    /// <returns>完成任务的Task对象</returns>
    Task InitializeAsync(ISampleProvider source);

    /// <summary>
    /// 设置效果器配置参数
    /// </summary>
    /// <typeparam name="T">参数类型</typeparam>
    /// <param name="key">参数名称（非空）</param>
    /// <param name="value">参数值</param>
    /// <exception cref="ArgumentException">当key为空时抛出</exception>
    void SetParameter<T>(string key, T value);

    /// <summary>
    /// 获取效果器配置参数
    /// </summary>
    /// <typeparam name="T">期望的参数类型</typeparam>
    /// <param name="key">参数名称（非空）</param>
    /// <returns>参数值</returns>
    /// <exception cref="ArgumentException">当key为空时抛出</exception>
    /// <exception cref="KeyNotFoundException">当参数不存在时抛出</exception>
    /// <exception cref="InvalidCastException">当类型不匹配时抛出</exception>
    T GetParameter<T>(string key);

    /// <summary>
    /// 链式设置音频源
    /// </summary>
    /// <param name="source">音频源提供者</param>
    /// <returns>当前效果器实例</returns>
    IAudioEffect WithSource(ISampleProvider source);

    /// <summary>
    /// 创建效果器配置的深拷贝
    /// </summary>
    /// <returns>新的效果器实例</returns>
    IAudioEffect Clone();

    /// <summary>
    /// 获取格式化的调试信息
    /// </summary>
    string DebugInfo { get; }

    /// <summary>
    /// 参数变更时触发的事件
    /// </summary>
    event EventHandler<ParameterChangedEventArgs> ParameterChanged;
}

public class ParameterChangedEventArgs(string key, object? value) : EventArgs
{
    public string Key { get; } = key;
    public object? Value { get; } = value;
}
