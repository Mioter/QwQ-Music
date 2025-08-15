using System;

namespace QwQ_Music.Common.Interfaces;

/// <summary>
///     音频播放接口，定义音频播放器的基本行为
/// </summary>
public interface IAudioPlay : IDisposable
{
    /// <summary>
    ///     是否静音
    /// </summary>
    bool IsMute { get; set; }

    /// <summary>
    ///     音量（范围：0 到 100）
    /// </summary>
    float Volume { get; set; }

    /// <summary>
    ///     播放位置
    /// </summary>
    double Position { get; set; }

    /// <summary>
    ///     播放速度（需 > 0f）
    /// </summary>
    float Speed { get; set; }

    /// <summary>
    ///     播放进度变化事件
    /// </summary>
    event EventHandler<double>? PositionChanged;

    /// <summary>
    ///     播放完成事件
    /// </summary>
    event EventHandler? PlaybackCompleted;

    /// <summary>
    ///     开始播放
    /// </summary>
    void Play();

    /// <summary>
    ///     暂停播放
    /// </summary>
    void Pause();

    /// <summary>
    ///     停止播放并释放资源
    /// </summary>
    void Stop();

    /// <summary>
    ///     跳转到指定位置（单位：秒）
    /// </summary>
    void Seek(double positionInSeconds);

    /// <summary>
    ///     设置音频文件并初始化
    /// </summary>
    void InitializeAudio(string filePath, double replayGain);
}
