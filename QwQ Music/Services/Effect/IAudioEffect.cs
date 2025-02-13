using NAudio.Wave;

namespace QwQ_Music.Services.Effect;

/// <summary>
/// 所有音频处理节点的统一接口。
/// 定义了音频效果器的基本行为和属性。
/// </summary>
public interface IAudioEffect : ISampleProvider
{
    /// <summary>
    /// 获取或设置音频源。
    /// 音频效果器将基于此源进行处理。
    /// </summary>
    ISampleProvider Source { get; set; }

    /// <summary>
    /// 获取效果器的名称。
    /// 名称用于标识效果器实例，便于调试和管理。
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// 设置效果器的配置参数。
    /// </summary>
    /// <param name="key">参数名称</param>
    /// <param name="value">参数值</param>
    void SetParameter(string key, object? value);

    /// <summary>
    /// 获取效果器的配置参数。
    /// </summary>
    /// <param name="key">参数名称</param>
    /// <returns>参数值</returns>
    object? GetParameter(string key);
    
    /// <summary>
    /// 设置音频源并返回当前实例。
    /// </summary>
    /// <param name="source">音频源</param>
    /// <returns>当前效果器实例</returns>
    IAudioEffect WithSource(ISampleProvider source);
}
