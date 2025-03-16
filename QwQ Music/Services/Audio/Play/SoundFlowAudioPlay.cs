using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Threading;
using QwQ_Music.Services.Audio.Effect.Base;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Components;
using SoundFlow.Enums;
using SoundFlow.Providers;

namespace QwQ_Music.Services.Audio.Play;

/// <summary>
/// 基于SoundFlow实现的音频播放器
/// </summary>
public class SoundFlowAudioPlay : IAudioPlay
{
    private readonly MiniAudioEngine _audioEngine;
    private SoundPlayer? _soundPlayer;
    private DispatcherTimer? _progressTimer;
    private float _volume = 1.0f;
    
    /// <summary>
    /// 用户效果器配置存储
    /// </summary>
    public Dictionary<string, EffectConfig> UserConfigs { get; set; } = new();

    /// <summary>
    /// 播放进度变化事件
    /// </summary>
    public event EventHandler<double>? PositionChanged;

    /// <summary>
    /// 播放完成事件
    /// </summary>
    public event EventHandler? PlaybackCompleted;

    /// <summary>
    /// 构造函数，初始化SoundFlow音频引擎
    /// </summary>
    public SoundFlowAudioPlay()
    {
        // 初始化SoundFlow音频引擎，使用44.1kHz采样率，仅支持播放功能
        _audioEngine = new MiniAudioEngine(44100, Capability.Playback);

    }

    /// <summary>
    /// 设置音量（范围：0.0 到 1.0）
    /// </summary>
    public void SetVolume(float volume)
    {
        _volume = Math.Clamp(volume, 0.0f, 1.0f);
        if (_soundPlayer != null)
        {
            _soundPlayer.Volume = _volume;
        }
    }

    /// <summary>
    /// 开始播放
    /// </summary>
    public void Play()
    {
        if (_soundPlayer == null)
            return;

        _soundPlayer.Play();

        // 启动进度定时器
        StartProgressTimer();
    }

    /// <summary>
    /// 暂停播放
    /// </summary>
    public void Pause()
    {
        if (_soundPlayer is not { State: PlaybackState.Playing })
            return;

        _soundPlayer.Pause();
        _progressTimer?.Stop();
    }

    /// <summary>
    /// 停止播放并释放资源
    /// </summary>
    public void Stop()
    {
        DisposeCurrentTrack();
    }

    /// <summary>
    /// 跳转到指定位置（单位：秒）
    /// </summary>
    public void Seek(double positionInSeconds)
    {
        if (_soundPlayer == null)
            return;

        // 确保位置在有效范围内
        positionInSeconds = Math.Clamp(positionInSeconds, 0, _soundPlayer.Duration);
        
        _soundPlayer.Seek((float)positionInSeconds);
        
    }

    /// <summary>
    /// 设置音频文件并初始化效果链
    /// </summary>
    public void SetAudioTrack(string filePath, double startingSeconds, double channelGains)
    {
        try
        {
            DisposeCurrentTrack();
            InitializeNewTrack(filePath, startingSeconds, channelGains);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"初始化音轨失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 初始化新音轨
    /// </summary>
    private void InitializeNewTrack(string audioFilePath, double startingSeconds, double replayGain)
    {
        
        // 创建文件流数据提供者
        var fileStream = File.OpenRead(audioFilePath);
        var dataProvider = new StreamDataProvider(fileStream);
        
        // 创建SoundPlayer并加载音频文件
        _soundPlayer = new SoundPlayer(dataProvider)
        {
            Volume = _volume,  // 设置音量
        };

        // 添加到主混音器
        Mixer.Master.AddComponent(_soundPlayer);

        // 如果需要从特定位置开始播放
        if (startingSeconds >= 0)
        {
            Seek(startingSeconds);
        }
        
        // 设置播放完成事件
        _soundPlayer.PlaybackEnded += OnPlaybackCompleted;
        
        // 初始化效果链
        InitializeEffects(replayGain);
        
        // 初始化进度定时器
        _progressTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _progressTimer.Tick += OnProgressTimerTick;
    }


    /// <summary>
    /// 初始化效果链
    /// </summary>
    private void InitializeEffects(double replayGain)
    {
        /*
        // 清空现有效果
        var reverb = new AlgorithmicReverbModifier { RoomSize = 0.8f, Wet = 0.2f };
        _soundPlayer?.AddModifier(reverb);
        */

    }

    /// <summary>
    /// 更新效果器启用状态
    /// </summary>
    public void UpdateEffectsEnabled(string effectName, bool value)
    {

    }

    /// <summary>
    /// 更新效果器参数
    /// </summary>
    public void UpdateEffectsParameters(string effectName, string parameter, object value)
    {

    }

    /// <summary>
    /// 带淡入效果的播放
    /// </summary>
    public void PlayWithFade()
    {

        Play();
    }

    /// <summary>
    /// 带淡出效果的停止
    /// </summary>
    public void StopWithFade()
    {

        Pause();
    }

    /// <summary>
    /// 添加新效果
    /// </summary>
    public void AddEffect(IAudioEffect effect)
    {

    }

    /// <summary>
    /// 移除效果
    /// </summary>
    public bool RemoveEffect(string effectName)
    {
        return false;
    }

    /// <summary>
    /// 获取效果实例
    /// </summary>
    public T? GetEffect<T>(string name) where T : class, IAudioEffect
    {

        return null;
    }

    /// <summary>
    /// 播放完成事件处理
    /// </summary>
    private void OnPlaybackCompleted(object? sender, EventArgs e)
    {
        // 触发播放完成事件
        PlaybackCompleted?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 进度定时器事件处理
    /// </summary>
    private void OnProgressTimerTick(object? sender, EventArgs e)
    {
        if (_soundPlayer is not { State: PlaybackState.Playing })
            return;
        
        // 触发位置变化事件
        PositionChanged?.Invoke(this, _soundPlayer.Time);
    }


    /// <summary>
    /// 启动进度定时器
    /// </summary>
    private void StartProgressTimer()
    {
        _progressTimer?.Start();
    }


    /// <summary>
    /// 释放当前音轨
    /// </summary>
    private void DisposeCurrentTrack()
    {
        if (_soundPlayer != null)
        {
            _soundPlayer.Stop();
            _soundPlayer.PlaybackEnded -= OnPlaybackCompleted;
            
            // 从主混音器中移除
            Mixer.Master.RemoveComponent(_soundPlayer);
            
            _soundPlayer = null;
        }
        
        // 停止并释放定时器
        if (_progressTimer != null)
        {
            _progressTimer.Stop();
            _progressTimer.Tick -= OnProgressTimerTick;
            _progressTimer = null;
        }

    }

    /// <summary>
    /// 释放所有资源
    /// </summary>
    public void Dispose()
    {
        DisposeCurrentTrack();
        
        // 释放音频引擎
        _audioEngine.Dispose();
        
        GC.SuppressFinalize(this);
    }
}