using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Timers;
using Avalonia.Threading;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using QwQ_Music.Services.Effect;
using Timer = System.Timers.Timer;

namespace QwQ_Music.Services.Audio;

public class AudioPlay : IDisposable
{
    private AudioFileReader? _audioFileReader;
    private DateTime _playStartTime; // 记录播放开始时间
    private DispatcherTimer? _progressTimer;
    private Timer? _fadeOutTimer; // 用于跟踪淡出定时器
    private float _volume = 1.0f;
    private WaveOutEvent? _waveOutEvent;
    private AudioEffectChain? _effectChain;
    private VolumeEffect? _volumeEffect; // 音量控制效果
    private FadeEffect? _fadeEffect; // 淡入淡出效果


    private readonly Dictionary<string, EffectConfig> _userConfigs = new();   // 用户配置存储
    private readonly Lock _lock = new(); // 确保线程安全

    /// <summary>
    /// 设置音量（范围：0.0 到 1.0）
    /// </summary>
    public void SetVolume(float volume)
    {
        _volume = Math.Clamp(volume, 0.0f, 1.0f);
        if (_volumeEffect != null) _volumeEffect.Volume = _volume; // 使用动态参数设置音量
    }

    /// <summary>
    /// 开始播放
    /// </summary>
    public void Play()
    {
        lock (_lock)
        {
            if (_waveOutEvent == null) return;

            // 停止并释放之前的淡出定时器
            StopFadeOutTimer();

            _progressTimer?.Start();
            _waveOutEvent.Play();
            _playStartTime = DateTime.Now; // 记录播放开始时间
        }
    }

    /// <summary>
    /// 暂停播放
    /// </summary>
    public void Pause()
    {
        lock (_lock)
        {
            _waveOutEvent?.Pause();
            _progressTimer?.Stop();
        }
    }

    /// <summary>
    /// 停止播放并释放资源
    /// </summary>
    public void Stop()
    {
        lock (_lock)
        {
            DisposeCurrentTrack();
        }
    }

    /// <summary>
    /// 跳转到指定位置（单位：秒）
    /// </summary>
    public void Seek(double positionInSeconds)
    {
        lock (_lock)
        {
            if (_audioFileReader == null || _waveOutEvent == null) return;

            var targetTime = TimeSpan.FromSeconds(positionInSeconds);
            _audioFileReader.CurrentTime = targetTime > _audioFileReader.TotalTime
                ? _audioFileReader.TotalTime
                : targetTime;
        }
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
    public void SetAudioTrack(string filePath, double startingSeconds, float[] channelGains)
    {
        lock (_lock)
        {
            try
            {
                DisposeCurrentTrack();
                InitializeNewTrack(filePath, startingSeconds, channelGains);
                UpdateEffects(_userConfigs); // 应用用户之前的配置
            }
            catch (Exception ex)
            {
                Console.WriteLine($"初始化音轨失败: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 初始化新音轨
    /// </summary>
    private void InitializeNewTrack(string audioFilePath, double startingSeconds, float[] replayGain)
    {
        _audioFileReader = new AudioFileReader(audioFilePath);

        // 验证增益数组
        int actualChannels = _audioFileReader.WaveFormat.Channels;
        if (replayGain.Length != actualChannels)
        {
            float fallbackGain = replayGain.Length > 0 ? replayGain[0] : 1.0f;
            replayGain = Enumerable.Repeat(fallbackGain, actualChannels).ToArray();
        }

        // 创建基础 Provider
        var offsetProvider = new OffsetSampleProvider(_audioFileReader)
        {
            SkipOver = CalculateSkipOver(startingSeconds, _audioFileReader),
        };

        // 初始化效果链
        _effectChain = new AudioEffectChain(offsetProvider);

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
        _progressTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(1000),
        };
        _progressTimer.Tick += OnProgressTimerTick;
    }

    /// <summary>
    /// 添加默认效果器
    /// </summary>
    private void AddDefaultEffects(float[] replayGain)
    {
        if (_effectChain == null) return;

        // 定义均衡器的频段配置（中心频率和增益）
        var bands = new (float frequency, float gain)[]
        {
            (31.25f, 0), (62.5f, 0), (125f, 0), // 低频段
            (250f, 0), (500f, 0), (1000f, 0), (2000f, 0), // 中频段
            (4000f, 0), (8000f, 0), (16000f, 0), // 高频段
        };

        _effectChain.AddEffect(new MultiChannelReplayGainEffect(replayGain)) // 多声道回放增益
            .AddEffect(new StereoEnhancementEffect()) // 立体声增强
            .AddEffect(new DistortionEffect()) // 失真
            .AddEffect(new ReverbEffect()) // 混响
            .AddEffect(new CompressorEffect()) // 压缩器
            .AddEffect(new EqualizerEffect(bands))  // 均衡器
            .AddEffect(new TremoloEffect()) // 颤音
            .AddEffect(new EchoesEffect()) // 延迟
            .AddEffect(new SpatialEffect()) // 空间
            .AddEffect(new RotatingEffect()) // 环绕
            .AddEffect(new FadeEffect()) // 淡入淡出
            .AddEffect(new VolumeEffect()); // 音量控制

        // 获取效果器实例
        _volumeEffect = _effectChain.GetEffect<VolumeEffect>("Volume");
        _fadeEffect = _effectChain.GetEffect<FadeEffect>("Fade");

        if (_volumeEffect != null) _volumeEffect.Volume = _volume;
    }

    /// <summary>
    /// 动态更新效果器配置
    /// </summary>
    /// <param name="effectsConfig">效果器配置</param>
    public void UpdateEffects(Dictionary<string, EffectConfig> effectsConfig)
    {
        lock (_lock)
        {
            foreach ((string? effectName, var effectConfig) in effectsConfig)
            {
                UpdateSpecificEffects(effectName, effectConfig);
            }
        }
    }

    /// <summary>
    /// 动态更新效果配置
    /// </summary>
    /// <param name="effectName">效果名</param>
    /// <param name="effectConfig">配置</param>
    public void UpdateSpecificEffects(string effectName, EffectConfig effectConfig)
    {
        lock (_lock)
        {
            _userConfigs[effectName] = effectConfig; // 保存用户配置
            
            var effect = _effectChain?.GetEffect<IAudioEffect>(effectName);
            if (effect == null) return;
            // 更新启用状态
            effect.Enabled = effectConfig.Enabled;

            // 更新参数
            foreach (var parameter in effectConfig.Parameters)
            {
                effect.SetParameter(parameter.Key, parameter.Value);
            }
        }
    }
    

    /// <summary>
    /// 刷新播放（变动效果链时调用）
    /// </summary>
    private void RefreshPlayback()
    {
        lock (_lock)
        {
            if (_waveOutEvent == null) return;

            bool wasPlaying = _waveOutEvent.PlaybackState == PlaybackState.Playing;
            _waveOutEvent.Stop();
            _waveOutEvent.Init(_effectChain?.GetOutput());
            if (wasPlaying) _waveOutEvent.Play();
        }
    }

    /// <summary>
    /// 计算跳过的时间
    /// </summary>
    private static TimeSpan CalculateSkipOver(double startingSeconds, AudioFileReader audioFileReader)
    {
        var targetTime = TimeSpan.FromSeconds(startingSeconds);
        return targetTime > audioFileReader.TotalTime ? audioFileReader.TotalTime : targetTime;
    }

    /// <summary>
    /// 释放当前音轨
    /// </summary>
    private void DisposeCurrentTrack()
    {
        lock (_lock)
        {
            _waveOutEvent?.Stop();
            _waveOutEvent?.Dispose();
            _waveOutEvent = null;

            _audioFileReader?.Dispose();
            _audioFileReader = null;

            _effectChain = null;
            _volumeEffect = null;
            _fadeEffect = null;

            // 停止并释放定时器
            StopProgressTimer();
            StopFadeOutTimer();
        }
    }

    /// <summary>
    /// 停止进度定时器
    /// </summary>
    private void StopProgressTimer()
    {
        lock (_lock)
        {
            if (_progressTimer == null) return;

            _progressTimer.Stop();
            _progressTimer.Tick -= OnProgressTimerTick;
            _progressTimer = null;
        }
    }

    /// <summary>
    /// 停止淡出定时器
    /// </summary>
    private void StopFadeOutTimer()
    {
        lock (_lock)
        {
            if (_fadeOutTimer == null) return;

            _fadeOutTimer.Stop();
            _fadeOutTimer.Elapsed -= OnFadeOutTimerElapsed;
            _fadeOutTimer.Dispose();
            _fadeOutTimer = null;
        }
    }

    /// <summary>
    /// 播放停止事件处理
    /// </summary>
    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        lock (_lock)
        {
            if (e.Exception != null)
            {
                Console.WriteLine($"播放错误: {e.Exception.Message}");
            }
            else if (_audioFileReader != null)
            {
                double currentPosition = _audioFileReader.CurrentTime.TotalSeconds;
                double totalTime = _audioFileReader.TotalTime.TotalSeconds;
                double timeElapsedSinceStart = (DateTime.Now - _playStartTime).TotalSeconds;

                // 如果当前位置接近总时长并且已经播放了一段时间，则认为是自然结束
                if (currentPosition >= totalTime - 0.5 && timeElapsedSinceStart > 0.5)
                {
                    PlaybackCompleted?.Invoke(this, EventArgs.Empty);
                }
            }
        }
    }

    /// <summary>
    /// 进度定时器事件处理
    /// </summary>
    private void OnProgressTimerTick(object? sender, EventArgs e)
    {
        lock (_lock)
        {
            if (_audioFileReader == null || _waveOutEvent == null) return;
            PositionChanged?.Invoke(this, _audioFileReader.CurrentTime.TotalSeconds);
        }
    }

    /// <summary>
    /// 带淡入效果的播放
    /// </summary>
    public void PlayWithFade()
    {
        lock (_lock)
        {
            _fadeEffect?.BeginFadeIn();
            Play();
        }
    }

    /// <summary>
    /// 带淡出效果的停止
    /// </summary>
    public void StopWithFade()
    {
        lock (_lock)
        {
            if (_fadeEffect is not { Enabled: true })
            {
                Pause();
                return;
            }

            _fadeEffect.BeginFadeOut();

            _fadeOutTimer = new Timer(_fadeEffect.GetParameter<double>("FadeOutMilliseconds"))
            {
                AutoReset = false,
            };
            _fadeOutTimer.Elapsed += OnFadeOutTimerElapsed;
            _fadeOutTimer.Start();
        }
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
        lock (_lock)
        {
            if (_effectChain == null) return;
            _effectChain.AddEffect(effect);
            RefreshPlayback();
        }
    }

    /// <summary>
    /// 插入新效果（受限版本）
    /// </summary>
    [Obsolete("此方法是不受支持的方法，请使用 AddEffect 方法。他会自动根据优先级管理顺序。",true)]
    public void InsertEffect(int index, IAudioEffect effect)
    {
        lock (_lock)
        {
            if (_effectChain == null) return;

            // 检查优先级是否符合插入位置的要求
            var effects = _effectChain.GetEffects();
            if (index > 0 && effect.Priority < effects[index - 1].Priority)
            {
                throw new InvalidOperationException("插入的效果器优先级低于前一个效果器，违反了优先级规则。");
            }
            if (index < effects.Count && effect.Priority > effects[index].Priority)
            {
                throw new InvalidOperationException("插入的效果器优先级高于后一个效果器，违反了优先级规则。");
            }

            _effectChain.InsertEffect(index, effect);
            RefreshPlayback();
        }
    }
    
    /// <summary>
    /// 移除效果
    /// </summary>
    [Obsolete("此方法计划废弃，因不再通过添加/移除管理效果，而是通过启用/禁用管理。", false)]
    public bool RemoveEffect(string effectName)
    {
        lock (_lock)
        {
            if (!(_effectChain?.RemoveEffect(effectName) ?? false)) return false;
            RefreshPlayback();
            return true;
        }
    }

    /// <summary>
    /// 获取效果实例
    /// </summary>
    public T? GetEffect<T>(string name) where T : class, IAudioEffect
    {
        lock (_lock)
        {
            return _effectChain?.GetEffect<T>(name);
        }
    }

    /// <summary>
    /// 释放所有资源
    /// </summary>
    public void Dispose()
    {
        lock (_lock)
        {
            DisposeCurrentTrack();
            GC.SuppressFinalize(this);
        }
    }
}
