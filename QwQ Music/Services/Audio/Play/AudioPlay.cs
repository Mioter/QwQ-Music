using System;
using System.IO;
using Avalonia.Threading;
using QwQ_Music.Models;
using QwQ_Music.Models.ConfigModel;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Components;
using SoundFlow.Enums;
using SoundFlow.Providers;

namespace QwQ_Music.Services.Audio.Play;

/// <summary>
/// 基于SoundFlow实现的音频播放器
/// </summary>
public class AudioPlay : IAudioPlay
{
    private readonly MiniAudioEngine _audioEngine;
    private SoundPlayer? _soundPlayer;
    private DispatcherTimer? _progressTimer;
    private DispatcherTimer? _fadeOutTimer; // 添加一个字段来跟踪当前的淡出定时器
    private float _volume = 1.0f;

    private readonly SoundEffectConfig _soundEffectConfig;

    /// <inheritdoc />
    public event EventHandler<double>? PositionChanged;

    /// <inheritdoc />
    public event EventHandler? PlaybackCompleted;

    /// <inheritdoc />
    public bool IsMute
    {
        get => _soundPlayer?.Mute ?? false;
        set
        {
            if (_soundPlayer != null) _soundPlayer.Mute = value;
        }
    }

    /// <summary>
    /// 构造函数，初始化SoundFlow音频引擎
    /// </summary>
    public AudioPlay()
    {
        // 初始化SoundFlow音频引擎，使用44.1kHz采样率，仅支持播放功能
        _audioEngine = new MiniAudioEngine(44100, Capability.Playback);
        _soundEffectConfig = ConfigInfoModel.SoundEffectConfig;
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

        if (_fadeOutTimer != null)
        {
            // 停止并清理定时器
            _fadeOutTimer.Stop();
            _fadeOutTimer.Tick -= FadeOutTimer_Tick;
            _fadeOutTimer = null;
        }
        
        // 检查淡入效果器是否启用
        if (_soundEffectConfig.FadeModifier.Enabled)
        {
            // 应用淡入效果
            _soundEffectConfig.FadeModifier.BeginFadeIn();
        }

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

        // 如果已经有一个淡出定时器在运行，先取消它
        if (_fadeOutTimer != null)
        {
            _fadeOutTimer.Stop();
            _fadeOutTimer.Tick -= FadeOutTimer_Tick;
            _fadeOutTimer = null;
        }

        // 检查淡出效果器是否启用
        if (_soundEffectConfig.FadeModifier.Enabled)
        {
            // 应用淡出效果
            _soundEffectConfig.FadeModifier.BeginFadeOut();
            
            // 创建一个延迟暂停的定时器，等待淡出效果完成
            _fadeOutTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(_soundEffectConfig.FadeModifier.FadeOutTimeMs),
            };
            
            _fadeOutTimer.Tick += FadeOutTimer_Tick;
            _fadeOutTimer.Start();
        }
        else
        {
            // 直接暂停
            _soundPlayer.Pause();
            _progressTimer?.Stop();
        }
    }

    /// <summary>
    /// 淡出定时器事件处理
    /// </summary>
    private void FadeOutTimer_Tick(object? sender, EventArgs e)
    {
        if (sender is not DispatcherTimer timer) 
            return;

        // 实际执行暂停操作
        if (_soundPlayer?.State == PlaybackState.Playing)
        {
            _soundPlayer.Pause();
            _progressTimer?.Stop();
        }
        
        // 停止并清理定时器
        timer.Stop();
        timer.Tick -= FadeOutTimer_Tick;
        _fadeOutTimer = null;
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
    
    /// <inheritdoc />
    public void InitializeAudio(string filePath, double channelGains)
    {
        try
        {
            DisposeCurrentTrack();
            InitializeNewTrack(filePath, channelGains);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"初始化音轨失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 初始化新音轨
    /// </summary>
    private void InitializeNewTrack(string audioFilePath, double replayGain)
    {
        // 创建文件流数据提供者
        var fileStream = File.OpenRead(audioFilePath);
        var dataProvider = new StreamDataProvider(fileStream);

        // 创建SoundPlayer并加载音频文件
        _soundPlayer = new SoundPlayer(dataProvider)
        {
            Volume = _volume,// 设置音量
        }; 

        _soundEffectConfig.ReplayGainModifier.Gain = (float)replayGain;

        // 重置淡入淡出效果器状态
        _soundEffectConfig.FadeModifier.Reset();

        InitializeEffects(_soundPlayer);
        
        // 添加到主混音器
        Mixer.Master.AddComponent(_soundPlayer);

        // 设置播放完成事件
        _soundPlayer.PlaybackEnded += OnPlaybackCompleted;

        // 初始化进度定时器
        _progressTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500),
        };
        _progressTimer.Tick += OnProgressTimerTick;
    }
    

    /// <summary>
    /// 初始化效果链
    /// </summary>
    private void InitializeEffects(SoundPlayer soundPlayer)
    {
        soundPlayer
            .AddModifier(_soundEffectConfig.ReplayGainModifier)
            .AddModifier(_soundEffectConfig.ReverbModifier)
            .AddModifier(_soundEffectConfig.DelayModifier)
            .AddModifier(_soundEffectConfig.FadeModifier)
            .AddModifier(_soundEffectConfig.RotatingModifier)
            .AddModifier(_soundEffectConfig.SpatialModifier)
            .AddModifier(_soundEffectConfig.CompressorModifier)
            .AddModifier(_soundEffectConfig.StereoEnhancementModifier)
            .AddModifier(_soundEffectConfig.TremoloModifier)
            .AddModifier(_soundEffectConfig.DistortionModifier)
            .AddModifier(_soundEffectConfig.ParametricEqualizer);
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
        // 清理淡出定时器
        if (_fadeOutTimer != null)
        {
            _fadeOutTimer.Stop();
            _fadeOutTimer.Tick -= FadeOutTimer_Tick;
            _fadeOutTimer = null;
        }

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
        
        ConfigInfoModel.SaveSoundEffectConfig();
        
        // 释放音频引擎
        _audioEngine.Dispose();
        
        GC.SuppressFinalize(this);
    }
}