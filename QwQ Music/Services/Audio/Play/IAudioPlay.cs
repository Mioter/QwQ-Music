using System;
using System.Collections.Generic;
using QwQ_Music.Services.Audio.Effect.Base;

namespace QwQ_Music.Services.Audio.Play;

/// <summary>
/// 音频播放接口，定义音频播放器的基本行为
/// </summary>
public interface IAudioPlay : IDisposable
{
    /// <summary>
    /// 用户效果器配置存储
    /// </summary>
    Dictionary<string, EffectConfig> UserConfigs { get;set; }

    /// <summary>
    /// 播放进度变化事件
    /// </summary>
    event EventHandler<double>? PositionChanged;

    /// <summary>
    /// 播放完成事件
    /// </summary>
    event EventHandler? PlaybackCompleted;

    /// <summary>
    /// 设置音量（范围：0.0 到 1.0）
    /// </summary>
    void SetVolume(float volume);

    /// <summary>
    /// 开始播放
    /// </summary>
    void Play();

    /// <summary>
    /// 暂停播放
    /// </summary>
    void Pause();

    /// <summary>
    /// 停止播放并释放资源
    /// </summary>
    void Stop();

    /// <summary>
    /// 跳转到指定位置（单位：秒）
    /// </summary>
    void Seek(double positionInSeconds);

    /// <summary>
    /// 设置音频文件并初始化效果链
    /// </summary>
    void SetAudioTrack(string filePath, double startingSeconds, double channelGains);

    /// <summary>
    /// 更新效果器启用状态
    /// </summary>
    /// <param name="effectName">效果器名称</param>
    /// <param name="value">启用状态</param>
    void UpdateEffectsEnabled(string effectName, bool value);

    /// <summary>
    /// 更新效果器参数
    /// </summary>
    /// <param name="effectName">效果器名称</param>
    /// <param name="parameter">参数名</param>
    /// <param name="value">参数值</param>
    void UpdateEffectsParameters(string effectName, string parameter, object value);

    /// <summary>
    /// 带淡入效果的播放
    /// </summary>
    void PlayWithFade();

    /// <summary>
    /// 带淡出效果的停止
    /// </summary>
    void StopWithFade();

    /// <summary>
    /// 添加新效果
    /// </summary>
    void AddEffect(IAudioEffect effect);

    /// <summary>
    /// 移除效果
    /// </summary>
    bool RemoveEffect(string effectName);

    /// <summary>
    /// 获取效果实例
    /// </summary>
    T? GetEffect<T>(string name) where T : class, IAudioEffect;
}