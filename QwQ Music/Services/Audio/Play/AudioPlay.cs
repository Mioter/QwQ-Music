using System;
using System.Collections.Generic;
using System.Timers;
using Avalonia.Threading;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using QwQ_Music.Services.Audio.Effect;
using QwQ_Music.Services.Audio.Effect.Base;
using Timer = System.Timers.Timer;

namespace QwQ_Music.Services.Audio.Play;

public class AudioPlay : IDisposable
{
    private MediaFoundationReader? _mediaFoundationReader;
    private DateTime _playStartTime; // 记录播放开始时间
    private DispatcherTimer? _progressTimer;
    private Timer? _fadeOutTimer; // 用于跟踪淡出定时器
    private float _volume = 1.0f;
    private WaveOutEvent? _waveOutEvent;
    private AudioEffectChain? _effectChain;
    private VolumeSampleProvider? _volumeSampleProvider; // 音量控制
    private FadeEffect? _fadeEffect; // 淡入淡出效果

    public Dictionary<string, EffectConfig> UserConfigs = new(); // 用户配置存储

    /// <summary>
    /// 设置音量（范围：0.0 到 1.0）
    /// </summary>
    public void SetVolume(float volume)
    {
        _volume = Math.Clamp(volume, 0.0f, 1.0f);
        if (_volumeSampleProvider != null)
            _volumeSampleProvider.Volume = _volume;
    }

    /// <summary>
    /// 开始播放
    /// </summary>
    public void Play()
    {
        if (_waveOutEvent == null)
            return;

        // 停止并释放之前的淡出定时器
        StopFadeOutTimer();

        _progressTimer?.Start();
        _waveOutEvent.Play();
        _playStartTime = DateTime.Now; // 记录播放开始时间
    }

    /// <summary>
    /// 暂停播放
    /// </summary>
    public void Pause()
    {
        _waveOutEvent?.Pause();
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
        if (_mediaFoundationReader == null || _waveOutEvent == null)
            return;

        var targetTime = TimeSpan.FromSeconds(positionInSeconds);
        _mediaFoundationReader.CurrentTime =
            targetTime > _mediaFoundationReader.TotalTime ? _mediaFoundationReader.TotalTime : targetTime;
    }

    /// <summary>
    /// 播放进度变化事件
    /// </summary>
    public event EventHandler<double>? PositionChanged;

    /// <summary>
    /// 播放完成事件
    /// </summary>
    public event EventHandler? PlaybackCompleted;

    /// <summary>
    /// 设置音频文件并初始化效果链
    /// </summary>
    public void SetAudioTrack(string filePath, double startingSeconds, double channelGains)
    {
        try
        {
            DisposeCurrentTrack();
            InitializeNewTrack(filePath, startingSeconds, channelGains);
            UpdateEffects(UserConfigs); // 应用用户之前的配置
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
        _mediaFoundationReader = new MediaFoundationReader(audioFilePath);
        var sampleProvider = new SampleChannel(_mediaFoundationReader); // 转换为 ISampleProvider

        // 从指定时间播放
        var offsetProvider = new OffsetSampleProvider(sampleProvider)
        {
            SkipOver = TimeSpan.FromSeconds(startingSeconds),
        };

        // 音量控制层
        _volumeSampleProvider = new VolumeSampleProvider(offsetProvider) { Volume = _volume };

        // 初始化效果链
        _effectChain = new AudioEffectChain(_volumeSampleProvider);

        // 添加默认效果器
        AddDefaultEffects(replayGain);

        // 初始化播放器
        _waveOutEvent = new WaveOutEvent
        {
            DesiredLatency = 100, // 设置较低的延迟以提高响应速度
        };

        _waveOutEvent.PlaybackStopped += OnPlaybackStopped;
        _waveOutEvent.Init(_effectChain.GetOutput());

        // 初始化进度定时器
        _progressTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _progressTimer.Tick += OnProgressTimerTick;
    }

    /// <summary>
    /// 添加默认效果器
    /// </summary>
    private void AddDefaultEffects(double replayGain)
    {
        if (_effectChain == null)
            return;

        _effectChain
            .AddEffect(new ReplayGainEffect(replayGain)) // 多声道回放增益
            .AddEffect(new StereoEnhancementEffect()) // 立体声增强
            .AddEffect(new DistortionEffect()) // 失真
            .AddEffect(new ReverbEffect()) // 混响
            .AddEffect(new CompressorEffect()) // 压缩器
            .AddEffect(new EqualizerEffect()) // 均衡器
            .AddEffect(new TremoloEffect()) // 颤音
            .AddEffect(new DelayEffect()) // 回声
            .AddEffect(new SpatialEffect()) // 空间
            .AddEffect(new RotatingEffect()) // 环绕
            .AddEffect(new FadeEffect()); // 淡入淡出

        // 获取效果器实例
        _fadeEffect = _effectChain.GetEffect<FadeEffect>("Fade");
    }

    /// <summary>
    /// 动态更新效果器配置
    /// </summary>
    /// <param name="effectsConfig">效果器配置</param>
    public void UpdateEffects(Dictionary<string, EffectConfig> effectsConfig)
    {
        foreach ((string? effectName, var effectConfig) in effectsConfig)
        {
            UpdateSpecificEffects(effectName, effectConfig);
        }
    }

    /// <summary>
    /// 动态更新效果配置
    /// </summary>
    /// <param name="effectName">效果名</param>
    /// <param name="effectConfig">配置</param>
    public void UpdateSpecificEffects(string effectName, EffectConfig effectConfig)
    {
        var effect = _effectChain?.GetEffect(effectName);
        if (effect == null)
            return;
        // 更新启用状态
        effect.Enabled = effectConfig.Enabled;

        // 更新参数
        foreach (var parameter in effectConfig.Parameters)
        {
            effect.SetParameter(parameter.Key, parameter.Value);
        }
    }

    public void UpdateEffectsEnabled(string effectName, bool value)
    {
        var effect = _effectChain?.GetEffect(effectName);

        if (effect == null)
            return;

        effect.Enabled = value;
    }

    public void UpdateEffectsParameters(string effectName, string parameter, object value)
    {
        var effect = _effectChain?.GetEffect(effectName);

        effect?.SetParameter(parameter, value);
    }

    /// <summary>
    /// 刷新播放（变动效果链时调用）
    /// </summary>
    private void RefreshPlayback()
    {
        if (_waveOutEvent == null)
            return;

        bool wasPlaying = _waveOutEvent.PlaybackState == PlaybackState.Playing;
        _waveOutEvent.Stop();
        _waveOutEvent.Init(_effectChain?.GetOutput());
        if (wasPlaying)
            _waveOutEvent.Play();
    }

    /// <summary>
    /// 释放当前音轨
    /// </summary>
    private void DisposeCurrentTrack()
    {
        _waveOutEvent?.Stop();
        _waveOutEvent?.Dispose();
        _waveOutEvent = null;

        _mediaFoundationReader?.Dispose();
        _mediaFoundationReader = null;

        _effectChain = null;
        _volumeSampleProvider = null;
        _fadeEffect = null;

        // 停止并释放定时器
        StopProgressTimer();
        StopFadeOutTimer();
    }

    /// <summary>
    /// 停止进度定时器
    /// </summary>
    private void StopProgressTimer()
    {
        if (_progressTimer == null)
            return;

        _progressTimer.Stop();
        _progressTimer.Tick -= OnProgressTimerTick;
        _progressTimer = null;
    }

    /// <summary>
    /// 停止淡出定时器
    /// </summary>
    private void StopFadeOutTimer()
    {
        if (_fadeOutTimer == null)
            return;

        _fadeOutTimer.Stop();
        _fadeOutTimer.Elapsed -= OnFadeOutTimerElapsed;
        _fadeOutTimer.Dispose();
        _fadeOutTimer = null;
    }

    /// <summary>
    /// 播放停止事件处理
    /// </summary>
    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        if (e.Exception != null)
        {
            Console.WriteLine($"播放错误: {e.Exception.Message}");
        }
        else if (_mediaFoundationReader != null)
        {
            double currentPosition = _mediaFoundationReader.CurrentTime.TotalSeconds;
            double totalTime = _mediaFoundationReader.TotalTime.TotalSeconds;
            double timeElapsedSinceStart = (DateTime.Now - _playStartTime).TotalSeconds;

            // 如果当前位置接近总时长并且已经播放了一段时间，则认为是自然结束
            if (currentPosition >= totalTime - 0.5 && timeElapsedSinceStart > 0.5)
            {
                PlaybackCompleted?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// 进度定时器事件处理
    /// </summary>
    private void OnProgressTimerTick(object? sender, EventArgs e)
    {
        if (_mediaFoundationReader == null || _waveOutEvent == null)
            return;
        PositionChanged?.Invoke(this, _mediaFoundationReader.CurrentTime.TotalSeconds);
    }

    /// <summary>
    /// 带淡入效果的播放
    /// </summary>
    public void PlayWithFade()
    {
        _fadeEffect?.BeginFadeIn();
        Play();
    }

    /// <summary>
    /// 带淡出效果的停止
    /// </summary>
    public void StopWithFade()
    {
        if (_fadeEffect is not { Enabled: true })
        {
            Pause();
            return;
        }

        _fadeEffect.BeginFadeOut();

        _fadeOutTimer = new Timer(_fadeEffect.GetParameter<double>("FadeOutTimeMs")) { AutoReset = false };
        _fadeOutTimer.Elapsed += OnFadeOutTimerElapsed;
        _fadeOutTimer.Start();
    }

    /// <summary>
    /// 响应淡出定时器
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void OnFadeOutTimerElapsed(object? sender, ElapsedEventArgs e) => Dispatcher.UIThread.InvokeAsync(Pause);

    /// <summary>
    /// 添加新效果
    /// </summary>
    public void AddEffect(IAudioEffect effect)
    {
        if (_effectChain == null)
            return;
        _effectChain.AddEffect(effect);
        RefreshPlayback();
    }

    /// <summary>
    /// 移除效果
    /// </summary>
    public bool RemoveEffect(string effectName)
    {
        if (!(_effectChain?.RemoveEffect(effectName) ?? false))
            return false;
        RefreshPlayback();
        return true;
    }

    /// <summary>
    /// 获取效果实例
    /// </summary>
    public T? GetEffect<T>(string name)
        where T : class, IAudioEffect
    {
        return _effectChain?.GetEffect<T>(name);
    }

    /// <summary>
    /// 释放所有资源
    /// </summary>
    public void Dispose()
    {
        DisposeCurrentTrack();
        GC.SuppressFinalize(this);
    }
}
