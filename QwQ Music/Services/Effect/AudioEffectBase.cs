using System;
using System.Collections.Generic;
using NAudio.Wave;

namespace QwQ_Music.Services.Effect;

/// <summary>
/// 基础装饰器实现
/// </summary>
public abstract class AudioEffectBase(ISampleProvider source) : IAudioEffect
{
    private ISampleProvider _source = source ?? throw new ArgumentNullException(nameof(source));
    private readonly Dictionary<string, object?> _parameters = new(); // 用于存储动态参数

    /// <summary>
    /// 音频源
    /// </summary>
    public ISampleProvider Source
    {
        get => _source;
        set => _source = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// 效果名称
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// 音频格式
    /// </summary>
    public WaveFormat WaveFormat => _source.WaveFormat;

    /// <summary>
    /// 读取音频数据并应用效果
    /// </summary>
    /// <param name="buffer">音频缓冲区</param>
    /// <param name="offset">起始偏移量</param>
    /// <param name="count">要读取的样本数</param>
    /// <returns>实际读取的样本数</returns>
    public abstract int Read(float[] buffer, int offset, int count);

    /// <summary>
    /// 检查音频格式是否兼容
    /// </summary>
    /// <param name="waveFormat">目标音频格式</param>
    protected void ValidateWaveFormat(WaveFormat waveFormat)
    {
        ArgumentNullException.ThrowIfNull(waveFormat);
        if (!waveFormat.Equals(_source.WaveFormat))
            throw new InvalidOperationException("音频格式不匹配");
    }

    /// <summary>
    /// 设置效果器的配置参数。
    /// </summary>
    /// <param name="key">参数名称</param>
    /// <param name="value">参数值</param>
    public void SetParameter(string key, object? value)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("参数名称不能为空", nameof(key));
        _parameters[key] = value;
    }

    /// <summary>
    /// 获取效果器的配置参数。
    /// </summary>
    /// <param name="key">参数名称</param>
    /// <returns>参数值</returns>
    public object? GetParameter(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("参数名称不能为空", nameof(key));
        return _parameters.GetValueOrDefault(key);
    }

    /// <summary>
    /// 设置音频源并返回当前实例。
    /// </summary>
    /// <param name="source">音频源</param>
    /// <returns>当前效果器实例</returns>
    public IAudioEffect WithSource(ISampleProvider source)
    {
        Source = source;
        return this;
    }
}