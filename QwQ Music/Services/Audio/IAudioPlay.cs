using System;

namespace QwQ_Music.Services.Audio;

/// <summary>
/// 音频播放接口，定义音频播放器的基本行为
/// </summary>
public interface IAudioPlay : IDisposable
{
    /// <summary>
    /// 播放进度变化事件
    /// </summary>
    event EventHandler<double>? PositionChanged;

    /// <summary>
    /// 播放完成事件
    /// </summary>
    event EventHandler? PlaybackCompleted;

    /// <summary>
    /// 是否静音
    /// </summary>
    bool IsMute { get; set; }

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
    /// 设置音频文件并初始化
    /// </summary>
    void InitializeAudio(string filePath, double channelGains);
    
}