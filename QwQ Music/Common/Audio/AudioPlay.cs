using System;
using System.IO;
using System.Linq;
using Avalonia.Threading;
using QwQ_Music.Common.Interfaces;
using QwQ_Music.Common.Manager;
using QwQ_Music.Models.ConfigModels;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Components;
using SoundFlow.Enums;
using SoundFlow.Providers;
using SoundFlow.Structs;

namespace QwQ_Music.Common.Audio;

/// <summary>
///     基于SoundFlow实现的音频播放器
/// </summary>
public class AudioPlay : IAudioPlay
{
    private DispatcherTimer? _fadeOutTimer; // 添加一个字段来跟踪当前的淡出定时器
    private AudioPlaybackDevice? _playerDevice;
    private DispatcherTimer? _progressTimer;
    private StreamDataProvider? _soundDataProvider;
    private SoundPlayer? _soundPlayer;

    private readonly SoundModifierConfig _soundModifier = ConfigManager.SoundModifierConfig;

    public MiniAudioEngine AudioEngine { get; } = new();

    /// <inheritdoc />
    public event EventHandler<double>? PositionChanged;

    /// <inheritdoc />
    public event EventHandler? PlaybackCompleted;

    /// <summary>
    /// 音频格式
    /// </summary>
    public AudioFormat AudioFormat { get; set; } = AudioFormat.DvdHq;
    
    /// <inheritdoc />
    public double Position
    {
        get
        {
            if (_soundPlayer != null)
                return _soundPlayer.Time;

            return -1;
        }
        set => Seek(value);
    }

    /// <inheritdoc />
    public bool IsMute
    {
        get;
        set
        {
            field = value;

            _soundPlayer?.Mute = value;
        }
    }

    /// <inheritdoc />
    public float Volume
    {
        get;
        set
        {
            field = Math.Clamp(value, 0.0f, 1.0f);

            _soundPlayer?.Volume = field;
        }
    } = 1.0f;

    /// <inheritdoc />
    public float Speed
    {
        get;
        set
        {
            if (value <= 0f)
                return;

            field = value;

            _soundPlayer?.PlaybackSpeed = field;
        }
    } = 1.0f;

    /// <summary>
    ///     开始播放
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
        if (_soundModifier.FadeModifier.Enabled )
        {
            // 应用淡入效果
            _soundModifier.FadeModifier.BeginFadeIn();
        }

        _soundPlayer.Play();

        // 启动进度定时器
        StartProgressTimer();
    }

    /// <summary>
    ///     暂停播放
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
        if (_soundModifier.FadeModifier.Enabled)
        {
            // 应用淡出效果
            _soundModifier.FadeModifier.BeginFadeOut();

            // 创建一个延迟暂停的定时器，等待淡出效果完成
            _fadeOutTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(_soundModifier.FadeModifier.FadeOutTimeMs),
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
    ///     停止播放并释放资源
    /// </summary>
    public void Stop()
    {
        _soundPlayer?.Stop();
    }

    /// <summary>
    ///     跳转到指定位置（单位：秒）
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
    ///     淡出定时器事件处理
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
    
    
    /// <inheritdoc />
    public void InitializeAudio(string filePath, double replayGain)
    {
        try
        {
            DisposeCurrentTrack();
            InitializeNewTrack(File.OpenRead(filePath), replayGain);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"初始化音轨失败: {ex.Message}");
        }
    }

    public void InitializeAudio(Stream audioStream, double replayGain)
    {
        try
        {
            DisposeCurrentTrack();
            InitializeNewTrack(audioStream, replayGain);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"初始化音轨失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     初始化新音轨
    /// </summary>
    private void InitializeNewTrack(Stream audioStream, double replayGain)
    {
        var defaultDevice = AudioEngine.PlaybackDevices.FirstOrDefault(x => x.IsDefault);
        
        _playerDevice = AudioEngine.InitializePlaybackDevice(defaultDevice, AudioFormat);

        _playerDevice.Start();

        _soundDataProvider = new StreamDataProvider(AudioEngine, AudioFormat, audioStream);

        _soundPlayer = new SoundPlayer(AudioEngine, AudioFormat, _soundDataProvider)
        {
            Volume = Volume,
            Mute = IsMute,
            PlaybackSpeed = Speed,
        };

        InitializeModifier(_soundPlayer,replayGain);
        _playerDevice.MasterMixer.AddComponent(_soundPlayer);
 
        // 设置播放完成事件
        _soundPlayer.PlaybackEnded += OnPlaybackCompleted;

        // 初始化进度定时器
        _progressTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(1000),
        };

        _progressTimer.Tick += OnProgressTimerTick;
    }

    /// <summary>
    ///     初始化效果链
    /// </summary>
    private void InitializeModifier(SoundPlayer soundPlayer, double replayGain)
    {
        _soundModifier.ReplayGainModifier.Gain = (float)replayGain;
        soundPlayer.AddModifier(_soundModifier.ReplayGainModifier);
        
        _soundModifier.FadeModifier.Reset();
        _soundModifier.FadeModifier.SampleRate = soundPlayer.Format.SampleRate;
        soundPlayer.AddModifier(_soundModifier.FadeModifier);
    }

    /// <summary>
    ///     播放完成事件处理
    /// </summary>
    private void OnPlaybackCompleted(object? sender, EventArgs e)
    {
        // 触发播放完成事件
        PlaybackCompleted?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    ///     进度定时器事件处理
    /// </summary>
    private void OnProgressTimerTick(object? sender, EventArgs e)
    {
        if (_soundPlayer is not { State: PlaybackState.Playing })
            return;

        // 触发位置变化事件
        PositionChanged?.Invoke(this, _soundPlayer.Time);
    }

    /// <summary>
    ///     启动进度定时器
    /// </summary>
    private void StartProgressTimer()
    {
        _progressTimer?.Start();
    }

    /// <summary>
    ///     释放当前音轨
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
            _soundPlayer.Dispose();
            _soundPlayer = null;
        }

        if (_playerDevice != null)
        {
            _playerDevice.Stop();
            _playerDevice.Dispose();
            _playerDevice = null;
        }

        // 停止并释放进度定时器
        if (_progressTimer != null)
        {
            _progressTimer.Stop();
            _progressTimer.Tick -= OnProgressTimerTick;
            _progressTimer = null;
        }

        _soundDataProvider?.Dispose();
    }
    
    /// <summary>
    ///     释放所有资源
    /// </summary>
    public void Dispose()
    {
        DisposeCurrentTrack();
        AudioEngine.Dispose();
        
        GC.SuppressFinalize(this);
    }
}
