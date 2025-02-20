using NAudio.Wave;
using System;
using System.Threading.Tasks;

namespace QwQ_Music.Services.Effect;

/// <summary>
/// 所有音频处理节点的统一接口。
/// 定义了音频效果器的基本行为和属性。
/// </summary>
public interface IAudioEffect : ISampleProvider
{
    /// <summary>
    /// 获取效果器的名称。
    /// 名称用于标识效果器实例，便于调试和管理。
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 是否启用该效果器。
    /// </summary>
    bool Enabled { get; set; }

    /// <summary>
    /// 效果器的优先级，用于确定效果器链中的执行顺序。
    /// </summary>
    int Priority { get; set; }

    /// <summary>
    /// 初始化效果器并设置音频源。
    /// </summary>
    /// <param name="source">音频源</param>
    void Initialize(ISampleProvider source);

    /// <summary>
    /// 异步初始化效果器并设置音频源。
    /// </summary>
    /// <param name="source">音频源</param>
    Task InitializeAsync(ISampleProvider source);

    /// <summary>
    /// 设置效果器的配置参数。
    /// </summary>
    /// <typeparam name="T">参数类型</typeparam>
    /// <param name="key">参数名称</param>
    /// <param name="value">参数值</param>
    void SetParameter<T>(string key, T value);

    /// <summary>
    /// 获取效果器的配置参数。
    /// </summary>
    /// <typeparam name="T">参数类型</typeparam>
    /// <param name="key">参数名称</param>
    /// <returns>参数值</returns>
    T GetParameter<T>(string key);

    /// <summary>
    /// 设置音频源并返回当前实例。
    /// </summary>
    /// <param name="source">音频源</param>
    /// <returns>当前效果器实例</returns>
    IAudioEffect WithSource(ISampleProvider source);

    /// <summary>
    /// 克隆当前效果器的配置。
    /// </summary>
    /// <returns>克隆的效果器实例</returns>
    IAudioEffect Clone();

    /// <summary>
    /// 返回当前效果器的调试信息。
    /// </summary>
    string DebugInfo { get; }

    /// <summary>
    /// 当效果器参数发生变化时触发的事件。
    /// </summary>
    event EventHandler<ParameterChangedEventArgs> ParameterChanged;
}

public class ParameterChangedEventArgs(string key, object? value) : EventArgs
{
    public string Key { get; } = key;
    public object? Value { get; } = value;

}