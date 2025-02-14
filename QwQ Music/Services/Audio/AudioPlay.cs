using System;
using System.Linq;
using System.Timers;
using Avalonia.Threading;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using QwQ_Music.Services.Effect;

namespace QwQ_Music.Services.Audio;

public class AudioPlay
{
    private AudioFileReader? _audioFileReader;
    private DateTime _playStartTime; // 记录播放开始的时间
    private DispatcherTimer? _progressTimer;

    private float _volume = 1.0f;

    private WaveOutEvent? _waveOutEvent;
    private AudioEffectChain? _effectChain;
    private VolumeEffect? _volumeEffect; // 将音量控制改造为效果
    private FadeEffect? _fadeEffect; // 将淡入淡出改造为效果

    public void SetVolume(float volume)
    {
        _volume = Math.Clamp(volume, 0.0f, 1.0f);

        if (_volumeEffect != null) _volumeEffect.Volume = _volume;

    }

    public void Play()
    {
        if (_waveOutEvent == null) return;

        _progressTimer?.Start();
        _waveOutEvent.Play();
        _playStartTime = DateTime.Now; // 记录播放开始时间
    }

    public void Pause()
    {
        _waveOutEvent?.Pause();
        _progressTimer?.Stop();
    }

    public void Stop()
    {
        DisposeCurrentTrack();
    }

    public void Seek(double positionInSeconds)
    {
        if (_audioFileReader == null || _waveOutEvent == null) return;

        var targetTime = TimeSpan.FromSeconds(positionInSeconds);
        if (targetTime > _audioFileReader.TotalTime)
            targetTime = _audioFileReader.TotalTime;

        _audioFileReader.CurrentTime = targetTime;
    }

    public event EventHandler<double>? PositionChanged;
    public event EventHandler? PlaybackCompleted;

    public void SetAudioTrack(string filePath, double startingSeconds, float[] channelGains)
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

    private void InitializeNewTrack(string audioFilePath, double startingSeconds, float[] replayGain)
    {
        _audioFileReader = new AudioFileReader(audioFilePath);

        // 验证增益数组（原有逻辑不变）
        int actualChannels = _audioFileReader.WaveFormat.Channels;
        if (replayGain.Length != actualChannels)
        {
            float fallbackGain = replayGain.Length > 0 ? replayGain[0] : 1.0f;
            replayGain = Enumerable.Repeat(fallbackGain, actualChannels).ToArray();
        }

        // 创建基础Provider
        var offsetProvider = new OffsetSampleProvider(_audioFileReader)
        {
            SkipOver = CalculateSkipOver(startingSeconds, _audioFileReader),
        };

        // 初始化效果链（重要修改部分）
        _effectChain = new AudioEffectChain(offsetProvider);

        // 添加默认效果链（按处理顺序添加）
        _effectChain
            .AddEffect(new MultiChannelReplayGainEffect(offsetProvider, replayGain)) // 多声道回放增益
            .AddEffect(new StereoEnhancementEffect(offsetProvider)) // 立体声增强
            /*.AddEffect(new ReverbEffect(offsetProvider)) // 混响*/
            .AddEffect(new VolumeEffect(offsetProvider)) // 将原有Volume改造为IAudioEffect
            .AddEffect(new FadeEffect(offsetProvider)); // 将淡入淡出改造为IAudioEffect

        // 保留原有Volume和Fade的引用以便控制
        _volumeEffect = _effectChain.GetEffect<VolumeEffect>("音量控制");
        _fadeEffect = _effectChain.GetEffect<FadeEffect>("淡入淡出");

        // 初始化播放器
        _waveOutEvent = new WaveOutEvent();
        _waveOutEvent.PlaybackStopped += OnPlaybackStopped;
        _waveOutEvent.Init(_effectChain.GetOutput());

        // 初始化定时器（原有逻辑不变）
        _progressTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500),
        };
        _progressTimer.Tick += OnProgressTimerTick;
    }

    private static TimeSpan CalculateSkipOver(double startingSeconds, AudioFileReader audioFileReader)
    {
        var targetTime = TimeSpan.FromSeconds(startingSeconds);
        return targetTime > audioFileReader.TotalTime ? audioFileReader.TotalTime : targetTime;
    }

    private void DisposeCurrentTrack()
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
        if (_progressTimer != null)
        {
            _progressTimer.Stop();
            _progressTimer.Tick -= OnProgressTimerTick;
        }
        _progressTimer = null;
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        if (e.Exception != null)
        {
            // 处理播放异常
            Console.WriteLine($"播放错误: {e.Exception.Message}");
        }
        else if (_audioFileReader != null)
        {
            // 检查是否接近歌曲末尾
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

    private static bool _dealThisTick = true;
    private void OnProgressTimerTick(object? sender, EventArgs e) {
        // ReSharper disable once AssignmentInConditionalExpression
        if (_dealThisTick = !_dealThisTick) return;
        if (_audioFileReader == null || _waveOutEvent == null) return;

        PositionChanged?.Invoke(this, _audioFileReader.CurrentTime.TotalSeconds);
    }

    public void PlayWithFade(int fadeInDuration)
    {
        _fadeEffect?.BeginFadeIn(fadeInDuration);
        Play();
    }

    public void StopWithFade(int fadeOutDuration)
    {
        if (_fadeEffect == null)
        {
            Pause();
            return;
        }
        _fadeEffect.BeginFadeOut(fadeOutDuration);
        // 自动停止逻辑
        var timer = new Timer(fadeOutDuration)
        {
            AutoReset = false,
        };
        timer.Elapsed += (_, _) => Dispatcher.UIThread.InvokeAsync(Pause);
        timer.Start();
    }

    // 添加新效果
    public void AddEffect(IAudioEffect effect)
    {
        if (_effectChain == null) return;

        _effectChain.AddEffect(effect);
        RefreshPlayback();
    }

    // 移除效果
    public bool RemoveEffect(string effectName)
    {
        return _effectChain?.RemoveEffect(effectName) ?? false;
    }

    // 获取效果实例
    public T? GetEffect<T>(string name) where T : class, IAudioEffect
    {
        return _effectChain?.GetEffect<T>(name);
    }

    // 刷新播放（切换效果时调用）
    private void RefreshPlayback()
    {
        if (_waveOutEvent == null) return;

        bool wasPlaying = _waveOutEvent.PlaybackState == PlaybackState.Playing;
        _waveOutEvent.Stop();
        _waveOutEvent.Init(_effectChain?.GetOutput());
        if (wasPlaying) _waveOutEvent.Play();
    }
    
}
