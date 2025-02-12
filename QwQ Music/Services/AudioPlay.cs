using System;
using System.Linq;
using Avalonia.Threading;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace QwQ_Music.Services;


public class AudioPlay
{
    private AudioFileReader? _audioFileReader;
    private DispatcherTimer? _progressTimer;
    private WaveOutEvent? _waveOutEvent;
    private VolumeSampleProvider? _volumeProvider; // 音量控制层
    private FadeInOutSampleProvider? _fadeProvider; // 淡入淡出层
    
    private float _volume = 1.0f;
    public void SetVolume(float volume)
    {
        _volume = Math.Clamp(volume, 0.0f, 1.0f);
        if (_volumeProvider != null)
        {
            _volumeProvider.Volume = _volume;
        }
    }
    
    public void Play()
    {
        if (_waveOutEvent == null) return;
        
        _progressTimer?.Start();
        _waveOutEvent.Play();
    }

    public void Pause()
    {
        _waveOutEvent?.Pause();
        _progressTimer?.Stop();
    }

    public void Stop()
    {
        _waveOutEvent?.Stop();
        _progressTimer?.Stop();
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

    private void InitializeNewTrack(string audioFilePath, double startingSeconds, float[] channelGains)
    {
        _audioFileReader = new AudioFileReader(audioFilePath);
        
        // 验证增益数组与声道数匹配
        int actualChannels = _audioFileReader.WaveFormat.Channels;
        if (channelGains.Length != actualChannels)
        {
            // 安全回退：使用首声道增益或默认值
            float fallbackGain = channelGains.Length > 0 ? channelGains[0] : 1.0f;
            channelGains = Enumerable.Repeat(fallbackGain, actualChannels).ToArray();
        }

        var targetTime = TimeSpan.FromSeconds(startingSeconds);
        var offsetProvider = new OffsetSampleProvider(_audioFileReader)
        {
            SkipOver = targetTime > _audioFileReader.TotalTime ? _audioFileReader.TotalTime : targetTime
        };

        // 构建处理链：原始音频 → 多声道增益 → 全局音量 → 淡入淡出
        var gainProvider = new MultiChannelGainSampleProvider(offsetProvider, channelGains);
        
        _volumeProvider = new VolumeSampleProvider(gainProvider)
        {
            Volume = _volume, // 全局音量控制
        };

        _fadeProvider = new FadeInOutSampleProvider(_volumeProvider);
        
        _waveOutEvent = new WaveOutEvent();
        _waveOutEvent.PlaybackStopped += OnPlaybackStopped;
        _waveOutEvent.Init(_fadeProvider);

        // 初始化定时器
        _progressTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000) };
        _progressTimer.Tick += OnProgressTimerTick;
    }


    private void DisposeCurrentTrack()
    {
        _waveOutEvent?.Stop();
        _waveOutEvent?.Dispose();
        _waveOutEvent = null;

        _audioFileReader?.Dispose();
        _audioFileReader = null;

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
    }
    

    private void OnProgressTimerTick(object? sender, EventArgs e)
    {
        if (_audioFileReader == null || _waveOutEvent == null) return;

        PositionChanged?.Invoke(this, _audioFileReader.CurrentTime.TotalSeconds);
    }

    public void PlayWithFade(int fadeInDuration)
    {
        if (_waveOutEvent == null || _fadeProvider == null) return;

        _fadeProvider.BeginFadeIn(fadeInDuration);
        Play(); // ✅ 淡入播放
    }

    public void StopWithFade(int fadeOutDuration)
    {
        if (_waveOutEvent == null || _fadeProvider == null) return;

        _fadeProvider.BeginFadeOut(fadeOutDuration); // ✅ 淡出

        // 使用定时器在淡出完成后停止
        var timer = new System.Timers.Timer(fadeOutDuration)
        {
            AutoReset = false,
        };
        timer.Elapsed += (_, _) => 
        {
            Dispatcher.UIThread.InvokeAsync(Pause);
            timer.Dispose();
        };
        timer.Start();
    }
}