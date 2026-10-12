using System.Collections.Concurrent;
using System.Diagnostics;
using Avalonia.Threading;
using QwQ_Music.Common.Interfaces;
using QwQ_Music.Common.Managers;
using QwQ_Music.Common.Services;
using QwQ_Music.Models.ConfigModels;
using SoundFlow.Abstracts;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Components;
using SoundFlow.Providers;
using SoundFlow.Structs;
using SoundFlow.Visualization;
using DrawerManager = QwQ_Music.Common.Managers.DrawerManager;
using ThreadState = System.Threading.ThreadState;
using Timer = System.Timers.Timer;

namespace QwQ_Music.Common.Audio;

public enum MediaPlaybackStatus {
    Changing, Fading, Playing, Paused, Stopped
}

/// <summary>
///     基于SoundFlow实现的音频播放器
/// </summary>
public sealed class AudioPlayer : IAudioPlayer {
    private readonly Thread _audioThread;

    private readonly BlockingCollection<Action> _commandQueue = new();
    private readonly PlayComponent _soundModifier = ConfigManager.SoundModifierConfig.PlayComponent;
    private readonly CancellationTokenSource _token;

    private StreamDataProvider? _soundDataProvider;
    private SoundPlayer? _soundPlayer;
    private SpectrumAnalyzer? _spectrumAnalyzer;

    public volatile MediaPlaybackStatus Status = MediaPlaybackStatus.Stopped;

    public AudioPlayer() {
        _token = new CancellationTokenSource();
        CancellationToken token = _token.Token;
        _audioThread = new Thread(() => {
            AudioEngine = new MiniAudioEngine();
            LoggerService.Debug("音频线程初始化完毕");
            while (!token.IsCancellationRequested)
                try {
                    _commandQueue.Take(token)();
                } catch (OperationCanceledException) {
                    LoggerService.Info("音频线程退出");
                } catch (Exception ex) {
                    LoggerService.Error("音频线程执行中出现错误", ex);
                }

            LoggerService.Info("音频播放线程已终止。");
        }) { Name = nameof(AudioPlayer), IsBackground = true, Priority = ThreadPriority.Highest };
        _audioThread.Start();
    }

    public bool IsDisposed => _audioThread.ThreadState == ThreadState.Stopped;

    private static MiniAudioEngine AudioEngine { get; set; } = null!;

    public Stream? Current { get; private set; }

    private AudioPlaybackDevice PlayerDevice {
        get {
            if (field is not { IsDisposed: false })
                field = InitializeDevice();

            return field;
        }
        set;
    }

    private Timer FadeoutTimer {
        get {
            // ReSharper disable once InvertIf
            if (field is null) {
                field = new Timer { AutoReset = false };
                field.Elapsed += OnFadeoutComplete;
            }

            return field;
        }
        set => field = value is null ? null : throw new InvalidOperationException();
    }

    private DispatcherTimer UpdateTimer {
        get {
            return field ??= new DispatcherTimer(
                TimeSpan.FromMilliseconds(1000),
                DispatcherPriority.Render,
                Dispatcher.UIThread,
                OnProgressTimerTick);
        }
        set => field = value is null ? null : throw new InvalidOperationException();
    }

    private DispatcherTimer SpecTimer {
        get {
            return field ??= new DispatcherTimer(
                TimeSpan.FromMilliseconds(ConfigManager.UiConfig.SpectrumConfig.UpdateIntervalMs),
                DispatcherPriority.Render,
                Dispatcher.UIThread,
                OnSpectrumVisualizer);
        }
        set => field = value is null ? null : throw new InvalidOperationException();
    }


    /// <summary>
    ///     音频格式
    /// </summary>
    public AudioFormat AudioFormat { get; set; } = AudioFormat.DvdHq;

    /// <inheritdoc />
    public event EventHandler<double>? PositionChanged;

    /// <inheritdoc />
    public event EventHandler? PlaybackCompleted;

    /// <inheritdoc />
    public double Position {
        get => _soundPlayer?.Time ?? -1;
        set => Seek(value);
    }

    /// <inheritdoc />
    public bool IsMute {
        get;
        set {
            field = value;
            _soundPlayer?.Volume = value ? 0 : Volume;

            LoggerService.Info(value ? "静音" : "恢复音量");
            // _soundPlayer?.Mute = value;
        }
    }

    /// <inheritdoc />
    public float Volume {
        get;
        set {
            field = value;
            _soundPlayer?.Volume = value;
            LoggerService.Info($"设定音量：{value}");
        }
    }

    /// <inheritdoc />
    public float Speed {
        get;
        set {
            field = value;
            _soundPlayer?.PlaybackSpeed = value;
            LoggerService.Info($"设定播放速度：{value}");
        }
    }

    /// <summary>
    ///     开始播放
    /// </summary>
    public void Play() {
        if (!CheckAccess()) {
            _commandQueue.Add(Play);
            return;
        }

        Debug.Assert(_soundPlayer is not null);
        ReloadDevice();
        if (Current is null) {
            LoggerService.Warning("当前音频流已不可用");
            return;
        }

        if (Status is MediaPlaybackStatus.Playing) {
            LoggerService.Warning("重复的播放请求");
            return;
        }

        // 检查淡入效果器是否启用
        if (_soundModifier.FadeModifier.Enabled) {
            Status = MediaPlaybackStatus.Fading;
            _soundModifier.FadeModifier.BeginFadeIn();
        }

        Status = MediaPlaybackStatus.Playing;
        PlayerDevice.Start();
        _soundPlayer.Play();
        SpecTimer.Start();

        UpdateTimer.Start();
        LoggerService.Info("继续播放");
    }

    /// <summary>
    ///     暂停播放
    /// </summary>
    public void Pause(bool instant = false) {
        if (!CheckAccess()) {
            _commandQueue.Add(() => Pause(instant));
            return;
        }

        if (Status is not MediaPlaybackStatus.Playing and not MediaPlaybackStatus.Fading) {
            // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
            if (Status is MediaPlaybackStatus.Paused)
                LoggerService.Warning("重复的暂停请求");
            else
                LoggerService.Warning($"错误的预期状态。预期{nameof(MediaPlaybackStatus.Paused)}，实际为{Status.ToString()}");
            return;
        }

        // 检查淡出效果器是否启用
        if (!instant && _soundModifier.FadeModifier.Enabled) {
            Status = MediaPlaybackStatus.Fading;
            // 应用淡出效果
            _soundModifier.FadeModifier.BeginFadeOut();
            // 重置淡出计时器，等待淡出效果完成
            ResetTimer(FadeoutTimer, _soundModifier.FadeModifier.FadeOutTimeMs);
            FadeoutTimer.Start();
        } else {
            // 直接暂停
            Debug.Assert(_soundPlayer is not null);
            _soundPlayer?.Pause();
            UpdateTimer.Stop();
            SpecTimer.Stop();
        }

        LoggerService.Info("暂停播放");
    }

    /// <summary>
    ///     停止播放并释放资源
    /// </summary>
    public void Stop() {
        if (!CheckAccess()) {
            _commandQueue.Add(Stop);
            return;
        }

        Pause();
        _soundPlayer?.PlaybackEnded -= OnPlaybackCompleted;
        UpdateTimer.Stop();
        SpecTimer.Stop();
        Current?.Dispose();
        Current = null;
        _soundDataProvider?.Dispose();
        _soundDataProvider = null;
        if (_soundPlayer is null)
            return;
        Debug.Assert(PlayerDevice.MasterMixer.Components.Count <= 1);
        foreach (SoundComponent comp in PlayerDevice.MasterMixer.Components)
            PlayerDevice.MasterMixer.RemoveComponent(comp);

        _soundPlayer.Dispose();
        Status = MediaPlaybackStatus.Stopped;
    }

    /// <summary>
    ///     跳转到指定位置（单位：秒）
    /// </summary>
    public void Seek(double positionInSeconds) {
        if (!CheckAccess()) {
            _commandQueue.Add(() => Seek(positionInSeconds));
            return;
        }

        _soundPlayer?.Seek((float)Math.Clamp(positionInSeconds, 0, _soundPlayer.Duration));
        LoggerService.Info($"跳转到{positionInSeconds}s处");
    }

    /// <inheritdoc />
    public void InitializeAudio(string filePath, double replayGain) {
        InitializeAudio(File.OpenRead(filePath), replayGain);
    }

    public void InitializeAudio(Stream audioStream, double replayGain) {
        if (!CheckAccess()) {
            _commandQueue.Add(() => InitializeAudio(audioStream, replayGain));
            return;
        }

        Stop();
        // if (AudioFormat != PlayerDevice.Format)
        //     TimeoutHelper.Timeout(2000, PlayerDevice.Dispose, () => PlayerDevice = null!);
        Current = audioStream;
        try {
            InitializeNewTrack(audioStream, replayGain);
        } catch (Exception e) {
            LoggerService.Error("初始化音轨失败", e);
        }
    }


    public bool CheckAccess() { return Thread.CurrentThread == _audioThread; }

    /// <summary>
    ///     频谱数据更新事件
    /// </summary>
    public event EventHandler<float[]>? SpectrumDataUpdated;

    /// <summary>
    ///     淡出定时器事件处理
    /// </summary>
    private void OnFadeoutComplete(object? sender, EventArgs e) {
        // 事件触发，不需要做验证
        if (Status is MediaPlaybackStatus.Playing) {
            LoggerService.Warning("警告：已忽略的暂停");
            return;
        }

        Debug.Assert(_soundPlayer is not null);
        Status = MediaPlaybackStatus.Paused;
        _soundPlayer.Pause();
        UpdateTimer.Stop();
        SpecTimer.Stop();
    }

    private void ResetTimer(Timer timer, double milliseconds = 1000) {
        if (!CheckAccess()) {
            _commandQueue.Add(() => ResetTimer(timer, milliseconds));
            return;
        }

        timer.Stop();
        if (milliseconds > 0)
            timer.Interval = milliseconds;
    }

    /// <summary>
    ///     初始化新音轨
    /// </summary>
    private void InitializeNewTrack(Stream audioStream, double replayGain) {
        if (!CheckAccess()) {
            _commandQueue.Add(() => InitializeNewTrack(audioStream, replayGain));
            return;
        }

        Status = MediaPlaybackStatus.Changing;
        ReloadDevice();
        _soundDataProvider = new StreamDataProvider(AudioEngine, AudioFormat, audioStream);
        LoggerService.Debug($"Volume:{Volume},Speed:{Speed}");
        _soundPlayer = new SoundPlayer(AudioEngine, AudioFormat, _soundDataProvider) {
            Volume = Volume, Mute = IsMute, PlaybackSpeed = Speed
        };

        // 设置播放完成事件
        _soundPlayer.PlaybackEnded += OnPlaybackCompleted;

        InitializeModifiers(_soundPlayer, replayGain);

        PlayerDevice.MasterMixer.AddComponent(_soundPlayer);

        // Spectrum
        // ReSharper disable once InvertIf
        if (ConfigManager.UiConfig.SpectrumConfig.IsEnabled) {
            _spectrumAnalyzer = new SpectrumAnalyzer(AudioFormat, ConfigManager.UiConfig.SpectrumConfig.FftSize);

            _soundPlayer.AddAnalyzer(_spectrumAnalyzer);

            SpecTimer.Interval = TimeSpan.FromMilliseconds(ConfigManager.UiConfig.SpectrumConfig.UpdateIntervalMs);
        }

        Status = MediaPlaybackStatus.Paused;
    }

    private void OnSpectrumVisualizer(object? sender, EventArgs eventArgs) {
        // 事件触发，不需要做验证
        if (_spectrumAnalyzer == null ||
            !ConfigManager.UiConfig.SpectrumConfig.IsEnabled ||
            !DrawerManager.Instance.IsMusicPlayerPanelVisible)
            return;

        float[] spectrumData = _spectrumAnalyzer.SpectrumData;

        if (spectrumData.Length <= 0)
            return;

        // 触发频谱数据更新事件
        SpectrumDataUpdated?.Invoke(this, spectrumData);
    }

    /// <summary>
    ///     初始化效果链
    /// </summary>
    private void InitializeModifiers(SoundPlayer soundPlayer, double replayGain) {
        if (!CheckAccess()) {
            _commandQueue.Add(() => InitializeModifiers(soundPlayer, replayGain));
            return;
        }

        _soundModifier.ReplayGainModifier.Gain = (float)replayGain;
        soundPlayer.AddModifier(_soundModifier.ReplayGainModifier);

        _soundModifier.FadeModifier.Reset();
        _soundModifier.FadeModifier.SampleRate = soundPlayer.Format.SampleRate;
        soundPlayer.AddModifier(_soundModifier.FadeModifier);

        foreach (ISoundModifierModel? soundModifier in SoundModifierManager.Default.SoundModifiers) {
            soundModifier.Initialize(AudioFormat);

            if (soundModifier.Modifier != null)
                soundPlayer.AddModifier(soundModifier.Modifier);
        }
    }

    /// <summary>
    ///     播放完成事件处理
    /// </summary>
    private void OnPlaybackCompleted(object? sender, EventArgs e) {
        LoggerService.Debug("音频播放完毕");
        // 事件触发，不需要做验证
        Stop();
        // 触发播放完成事件
        PlaybackCompleted?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    ///     进度定时器事件处理
    /// </summary>
    private void OnProgressTimerTick(object? sender, EventArgs e) {
        // 事件触发，不需要做验证

        Debug.Assert(_soundPlayer is not null);
        Debug.Assert(Status is MediaPlaybackStatus.Playing or MediaPlaybackStatus.Fading);

        ReloadDevice();

        // 触发位置变化事件
        PositionChanged?.Invoke(this, _soundPlayer.Time);
    }

    private AudioPlaybackDevice InitializeDevice() {
        AudioEngine.UpdateAudioDevicesInfo();
        return AudioEngine.InitializePlaybackDevice(
            AudioEngine.PlaybackDevices.FirstOrDefault(
                x => x.Name == ConfigManager.PlayerConfig.DefaultDevice,
                AudioEngine.PlaybackDevices.Single(x => x.IsDefault)),
            AudioFormat,
            ConfigManager.PlayerConfig.DeviceConfig);
    }

    public void ReloadDevice() {
        if (!CheckAccess()) {
            _commandQueue.Add(ReloadDevice);
            return;
        }

        AudioEngine.UpdateAudioDevicesInfo();
        DeviceInfo targetDeviceInfo;
        // TODO [SettingsPage] Default Device
        if (ConfigManager.PlayerConfig.DefaultDevice is not null)
            targetDeviceInfo = AudioEngine.PlaybackDevices.FirstOrDefault(
                x => x.Name == ConfigManager.PlayerConfig.DefaultDevice,
                AudioEngine.PlaybackDevices.Single(x => x.IsDefault));
        else
            targetDeviceInfo = AudioEngine.PlaybackDevices.Single(x => x.IsDefault);

        if (PlayerDevice.Info?.Id == targetDeviceInfo.Id && PlayerDevice.Info?.Name == targetDeviceInfo.Name)
            return;

        bool isPlaying = Status is MediaPlaybackStatus.Playing;
        if (isPlaying)
            Pause();


        PlayerDevice = AudioEngine.SwitchDevice(
            PlayerDevice,
            targetDeviceInfo,
            ConfigManager.PlayerConfig.DeviceConfig);


        if (isPlaying)
            Play();
    }

    /// <summary>
    ///     释放所有资源
    /// </summary>
    public void Dispose() {
        if (IsDisposed) {
            LoggerService.Warning("额外的AudioPlayer Dispose调用。已忽略");
            return;
        }

        Stop();
        using (_commandQueue) {
            _token.Cancel();
            _commandQueue.CompleteAdding();
            _audioThread.Join();
        }

        FadeoutTimer.Dispose();
        FadeoutTimer = null!;
        SpecTimer.Stop();
        SpecTimer = null!;
        UpdateTimer.Stop();
        UpdateTimer = null!;
        PlayerDevice.Dispose();
        PlayerDevice = null!;
        Debug.Assert(_audioThread.ThreadState == ThreadState.Stopped);
        PlaybackCompleted = null;
        PositionChanged = null;
        GC.SuppressFinalize(this);
    }

    ~AudioPlayer() { Dispose(); }
}