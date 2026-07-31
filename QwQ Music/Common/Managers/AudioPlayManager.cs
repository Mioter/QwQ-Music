using System.Runtime.CompilerServices;
using System.Timers;
using Avalonia.Collections;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Common.Audio;
using QwQ_Music.Common.Services;
using QwQ_Music.Common.Services.Databases;
using QwQ_Music.Models;
using QwQ_Music.Models.ConfigModels;
using QwQ_Music.Models.Enums;
using SystemMediaInterop;
using SystemSleepInhibitor;
using Timer = System.Timers.Timer;

namespace QwQ_Music.Common.Managers;

public class MusicItemChangedEventArgs(PlaylistItemModel oldItem, PlaylistItemModel newItem) : EventArgs {
    public readonly PlaylistItemModel NewItem = newItem;
    public readonly PlaylistItemModel OldItem = oldItem;
}

public sealed partial class AudioPlayManager : ObservableObject, IAsyncDisposable {
    public static AudioPlayManager Instance { get; } = new();

    #region 属性和字段

    public PlaylistManager PlaylistManager => PlaylistManager.Instance;

    public AudioPlayer AudioPlayer { get; } = new();
    private readonly AudioPreprocessor _audioPreprocessor;

    public readonly ISystemSleepInhibitorImpl Inhibitor = SleepInhibitor.CreateInhibitor(Program.AppId);

    public readonly ISystemMediaControlImpl SystemMediaControl =
        SystemMediaInterop.SystemMediaControl.CreateSystemMediaControl(
            Program.AppId,
            new SystemSideEffectConfig { Windows = { CreateStartMenuShortcut = true } });

    private readonly Timer _lrcTimer;

    public PlaylistItemModel CurrentMusicItem {
        get => PlaylistManager.Instance.CurrentItem;
        set {
            PlaylistManager.Instance.CurrentItem = value;
            OnPropertyChanged();
        }
    }

    public bool IsPlaying {
        get;
        set {
            if (!SetProperty(ref field, value))
                return;
            OnPropertyChanged();
            PlaybackStateChanged?.Invoke(this, value);
        }
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PreviousMusicCommand))]

    public partial bool IsPreviousEnabled { get; private set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextMusicCommand))]
    public partial bool IsNextEnabled { get; private set; }

    partial void OnIsPreviousEnabledChanged(bool value) { SystemMediaControl.IsPreviousEnabled = value; }

    partial void OnIsNextEnabledChanged(bool value) { SystemMediaControl.IsNextEnabled = value; }

    public static PlayerConfig PlayerConfig => ConfigManager.PlayerConfig;

    [ObservableProperty]
    public partial LyricsModel LyricsModel { get; set; } = new();

    private double _position;

    public void ClearPlaylist() {
        PlaylistManager.Clear();
        UpdateSequenceControlStatus();
    }

    public void UpdateCurrentLyric() {
        LyricsModel.UpdateLyricsIndex(_position);
        // 当播放位置改变时，重新设置歌词定时器
        if (IsPlaying)
            UpdateLyricsTimer();
    }

    public double Position {
        get => _position;
        set {
            _position = value;
            AudioPlayer.Seek(value);
            SystemMediaControl.Position = TimeSpan.FromSeconds(value);
            OnPropertyChanged();
            UpdateCurrentLyric();
        }
    }

    public int Volume {
        get => PlayerConfig.Volume;
        set {
            int result = Math.Clamp(value, 0, 100);

            if (result == PlayerConfig.Volume)
                return;

            PlayerConfig.Volume = result;
            SystemMediaControl.Volume = result;
            AudioPlayer.Volume = NormalizeVolume(result);

            IsMuted = result == 0f;
            OnPropertyChanged();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float NormalizeVolume(int volume) { return volume / 100f; }

    public bool IsMuted {
        get => PlayerConfig.IsMuted;
        set {
            if (value == PlayerConfig.IsMuted)
                return;

            PlayerConfig.IsMuted = value;
            AudioPlayer.IsMute = value;

            OnPropertyChanged();
        }
    }

    public float Speed {
        get => PlayerConfig.PlaybackSpeed;
        set {
            float result = Math.Clamp(value, 0.5f, 1.5f);

            if (Math.Abs(PlayerConfig.PlaybackSpeed - result) < 1e-6f)
                return;

            PlayerConfig.PlaybackSpeed = result;
            SystemMediaControl.PlaybackSpeed = result;
            OnPropertyChanged();
            AudioPlayer.Speed = result;
        }
    }

    public double LyricOffset {
        get => LyricsModel.Offset;
        set {
            LyricsModel.Offset = value;
            CurrentMusicItem.Model.LyricOffset = value;

            OnPropertyChanged();
        }
    }

    #endregion

    #region 事件

    public event EventHandler<bool>? PlaybackStateChanged;

    public event EventHandler<MusicItemChangedEventArgs>? MusicItemChanged;

    #endregion

    #region 音频处理

    private async Task SetCurrentMusicItemAsync(PlaylistItemModel musicItem, bool restart) {
        await LoggerService.DebugAsync($"正在切换音频：由《{CurrentMusicItem.Model.Title}》切换到《{musicItem.Model.Title}》。")
                           .ConfigureAwait(false);
        Pause(false);
        try {
            Position = 0;

            if (VerifyMusicItem(musicItem)) {
                await musicItem.Model.LoadCurrentAsync().ConfigureAwait(false);
                LyricsModel = new LyricsModel { Offset = musicItem.Model.LyricOffset, Lyrics = musicItem.Model.Lyrics };
                await _audioPreprocessor.UpdateMusicPlayProgressAsync(musicItem.Model, restart).ConfigureAwait(false);
                await _audioPreprocessor.InitializeAudioTrackAsync(musicItem.Model).ConfigureAwait(false);
                Position = musicItem.Model.Record.TotalSeconds;
                _audioPreprocessor.InitialTime = musicItem.Model.Record;
                LyricOffset = musicItem.Model.LyricOffset;
            }

            CurrentMusicItem.Model.DisposeCurrent();

            PlaylistItemModel oldItem = CurrentMusicItem;
            CurrentMusicItem = musicItem;

            UpdateSequenceControlStatus();
            MusicItemChanged?.Invoke(this, new MusicItemChangedEventArgs(oldItem, musicItem));
            await LoggerService.InfoAsync($"已切换到《{musicItem.Model.Title}》。").ConfigureAwait(false);
        } catch (Exception ex) {
            Pause(true);

            await LoggerService.ErrorAsync($"初始化新音轨失败:\n{ex.Message}\n{ex.StackTrace}").ConfigureAwait(false);

            NotificationService.Error(
                "播放失败",
                $"初始化新音轨失败: {ex.Message}\n可能的原因: 当前{musicItem.Model.EncodingFormat}格式不支持解码");
        }
    }

    #endregion

    #region 其他

    private void UpdateSequenceControlStatus() {
        if (Dispatcher.UIThread.CheckAccess())
            Updater();
        else
            Dispatcher.UIThread.Post(Updater, DispatcherPriority.Render);
        return;

        void Updater() {
            if (PlaylistManager.ActualPlaylist.Count == 0) {
                IsPreviousEnabled = false;
                IsNextEnabled = false;
                return;
            }

            IsPreviousEnabled = PlaylistManager.PlayMode is not PlayMode.Sequential ||
                                CurrentMusicItem != PlaylistManager.ActualPlaylist.FirstOrDefault();
            IsNextEnabled = PlaylistManager.PlayMode is not PlayMode.Sequential ||
                            CurrentMusicItem != PlaylistManager.ActualPlaylist.LastOrDefault();
        }
    }

    /// <summary>
    ///     注册热键功能
    /// </summary>
    private void RegisterHotkeyFunctions() {
        LoggerService.Debug("正在注册音频快捷键...");
        HotkeyService.RegisterFunctionAction(HotkeyFunction.Previous, () => PreviousMusicCommand.Execute(null));
        HotkeyService.RegisterFunctionAction(HotkeyFunction.Next, () => NextMusicCommand.Execute(null));
        HotkeyService.RegisterFunctionAction(HotkeyFunction.TogglePlay, () => TogglePlayStateCommand.Execute(null));
        HotkeyService.RegisterFunctionAction(HotkeyFunction.ToggleMute, () => ToggleMuteCommand.Execute(null));
        HotkeyService.RegisterFunctionAction(HotkeyFunction.SwitchPlayMode, () => TogglePlayModeCommand.Execute(null));
        HotkeyService.RegisterFunctionAction(
            HotkeyFunction.VolumeUp,
            () => {
                if (Volume < 100)
                    Volume += 5;
            });
        HotkeyService.RegisterFunctionAction(
            HotkeyFunction.VolumeDown,
            () => {
                if (Volume > 0)
                    Volume -= 5;
            });
        HotkeyService.RegisterFunctionAction(HotkeyFunction.Replay, () => ReplayCommand.Execute(null));
        HotkeyService.RegisterFunctionAction(
            HotkeyFunction.ShowPlaylistInfo,
            () => {
                NotificationService.Info(
                    "你知道吗？",
                    $"当前播放列表有: {PlaylistManager.Instance.Count} 首音乐！\n" +
                    $"现在正在播放第 {PlaylistManager.Instance.CurrentIndex} 首");
            });
        HotkeyService.RegisterFunctionAction(
            HotkeyFunction.ShowAudioInfo,
            () => {
                NotificationService.Info(
                    "你知道吗？",
                    $"{(IsPlaying ? "正在播放" : "已暂停")}的音乐叫做: {CurrentMusicItem.Model.Title} 哦！\n" + $"你的音量是: {Volume}% ");
            });
        LoggerService.Debug("快捷键音频注册完毕。");
    }

    private void UnregisterHotkeyFunctions() {
        LoggerService.Debug("正在注销音频快捷键...");
        HotkeyService.UnregisterFunctionAction(HotkeyFunction.Previous);
        HotkeyService.UnregisterFunctionAction(HotkeyFunction.Next);
        HotkeyService.UnregisterFunctionAction(HotkeyFunction.TogglePlay);
        HotkeyService.UnregisterFunctionAction(HotkeyFunction.ToggleMute);
        HotkeyService.UnregisterFunctionAction(HotkeyFunction.SwitchPlayMode);
        HotkeyService.UnregisterFunctionAction(HotkeyFunction.VolumeUp);
        HotkeyService.UnregisterFunctionAction(HotkeyFunction.VolumeDown);
        HotkeyService.UnregisterFunctionAction(HotkeyFunction.Replay);
        HotkeyService.UnregisterFunctionAction(HotkeyFunction.ShowPlaylistInfo);
        HotkeyService.UnregisterFunctionAction(HotkeyFunction.ShowAudioInfo);
        LoggerService.Debug("音频快捷键注销完毕。");
    }

    #endregion

    #region 初始化与终结

    private void UpdateSystemMedia(object? sender, MusicItemChangedEventArgs args) {
        SystemMediaControl.UpdateInfoAsync(args.NewItem).ConfigureAwait(false);
    }

    private AudioPlayManager() {
        _audioPreprocessor = new AudioPreprocessor(AudioPlayer);

        AudioPlayer.Volume = NormalizeVolume(Volume);
        AudioPlayer.IsMute = IsMuted;
        AudioPlayer.Speed = Speed;
        MusicItemChanged += UpdateSystemMedia;
        RegisterSystemMediaControlHandlers();

        AudioPlayer.PositionChanged += OnPositionChanged;
        AudioPlayer.PlaybackCompleted += AudioPlayerOnPlaybackCompleted;

        // 初始化歌词滚动定时器
        _lrcTimer = new Timer();
        _lrcTimer.Elapsed += OnLrcTimerElapsed;
        _lrcTimer.AutoReset = false;

        // 注册热键功能
        RegisterHotkeyFunctions();
    }

    #endregion

    #region 事件处理

    private void OnPositionChanged(object? sender, double positionInSeconds) {
        CurrentMusicItem.Model.Record = TimeSpan.FromSeconds(positionInSeconds);
        _position = positionInSeconds;
        SystemMediaControl.Position = TimeSpan.FromSeconds(positionInSeconds);
        OnPropertyChanged(nameof(Position));
        UpdateCurrentLyric();
    }

    private void UpdateLyricsTimer() {
        if (!IsPlaying)
            return;

        // 停止当前定时器
        _lrcTimer.Stop();

        // 计算到下一句歌词的时间间隔
        double nextInterval = LyricsModel.GetNextLyricsInterval(AudioPlayer.Position);

        if (nextInterval <= 0)
            return;

        // 设置定时器间隔为到下一句歌词的时间
        _lrcTimer.Interval = nextInterval * 1000; // 转换为毫秒
        _lrcTimer.Start();
    }

    private void OnLrcTimerElapsed(object? sender, ElapsedEventArgs e) {
        // 更新当前歌词
        LyricsModel.UpdateLyricsIndex(AudioPlayer.Position);

        // 计算到下一句歌词的时间间隔
        double nextInterval = LyricsModel.GetNextLyricsInterval(AudioPlayer.Position);

        if (!(nextInterval > 0))
            return;

        // 设置定时器间隔为到下一句歌词的时间
        _lrcTimer.Interval = nextInterval * 1000; // 转换为毫秒
        _lrcTimer.Start();
    }

    private void AudioPlayerOnPlaybackCompleted(object? sender, EventArgs e) {
        try {
            CurrentMusicItem.Model.Record = TimeSpan.Zero;

            // 根据播放模式处理播放完成后的行为
            if (PlayerConfig.PlayMode == PlayMode.SingleLoop) {
                LoggerService.Debug("播放模式为单曲循环，即将重新播放");
                Replay(); // 单曲循环模式下，重新播放当前歌曲
            } else if (PlayerConfig.AutoSwitchNext) {
                LoggerService.Debug("自动切换到下一首");
                NextMusicAsync(false).ContinueWith(LoggerService.HandleException).ConfigureAwait(false);
            } else {
                LoggerService.Debug("由于自动切换关闭，已暂停。");
                Pause(true);
            }
        } catch (Exception ex) {
            NotificationService.Error($"音频播放完成后切换下一首音频时遇到错误：{ex.Message}");
            LoggerService.Error($"音频播放完成后切换下一首音频时遇到错误：{ex.Message}\n{ex.StackTrace}");
        }
    }

    #endregion

    #region 播放控制方法

    [RelayCommand]
    public void TogglePlayState() {
        TogglePlayStateAsync().ContinueWith(LoggerService.HandleException).ConfigureAwait(false);
    }

    public async Task TogglePlayStateAsync() {
        if (MusicItemsManager.All.Count == 0) {
            NotificationService.Info("音乐库中一首音乐也没有啦，需要我为你播放一首空空如也吗？");

            return;
        }

        if (CurrentMusicItem == PlaylistItemModel.RefDefault)
            await SetCurrentMusicItemAsync(PlaylistManager.First(), true).ConfigureAwait(false);

        if (VerifyMusicItem(CurrentMusicItem))
            OnPlayingChanged(!IsPlaying, true);
    }

    [RelayCommand]
    public void PlayMusic(PlaylistItemModel? musicItem) {
        SetMusicAsync(musicItem, true, true).ContinueWith(LoggerService.HandleException).ConfigureAwait(false);
    }

    public async Task SetMusicAsync(PlaylistItemModel? musicItem, bool isPlaynow, bool isUserRequested) {
        if (musicItem is not { } item)
            return;
        bool isValid = VerifyMusicItem(musicItem);
        if (CurrentMusicItem.Equals(item) && isValid) {
            OnPlayingChanged(!IsPlaying, true);
        } else {
            await SetCurrentMusicItemAsync(item, !isPlaynow).ConfigureAwait(false);
            if (isValid)
                OnPlayingChanged(isPlaynow, isUserRequested);
        }
    }

    public async Task PreviousMusicAsync(bool isUserRequested) {
        await SetAndPlayAsync(GetMusicItemIndex(PlaylistManager.Instance.CurrentIndex, -1), isUserRequested)
            .ConfigureAwait(false);
    }


    public async Task NextMusicAsync(bool isUserRequested) {
        var next = GetMusicItemIndex(PlaylistManager.Instance.CurrentIndex, 1);
        if (PlayerConfig.PlayMode is PlayMode.Sequential && next == 0) {
            NotificationService.Info("列表播放结束：歌单已播放完毕！");
            await SetAndPlayAsync(-1, true).ConfigureAwait(false);
            return;
        }

        await SetAndPlayAsync(GetMusicItemIndex(PlaylistManager.Instance.CurrentIndex, 1), isUserRequested)
            .ConfigureAwait(false);
    }

    [RelayCommand(CanExecute = nameof(IsPreviousEnabled))]
    public void PreviousMusic() {
        PreviousMusicAsync(true).ContinueWith(LoggerService.HandleException).ConfigureAwait(false);
    }

    [RelayCommand(CanExecute = nameof(IsNextEnabled))]
    public void NextMusic() { NextMusicAsync(true).ContinueWith(LoggerService.HandleException).ConfigureAwait(false); }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Pause(bool isUserRequested) {
        IsPlaying = false;
        if (isUserRequested && ConfigManager.SystemConfig.KeepSystemAwake)
            Dispatcher.UIThread.Post(
                () => _ = Inhibitor.RestoreAsync().ContinueWith(LoggerService.HandleException).ConfigureAwait(true),
                DispatcherPriority.Background);
        AudioPlayer.Pause();
        _lrcTimer.Stop();
        if (isUserRequested && ConfigManager.LyricConfig.DesktopLyric.IsAutoFade)
            DesktopLyricsService.TryFadeOut();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Play(bool isUserRequested) {
        IsPlaying = true;
        if (isUserRequested && ConfigManager.LyricConfig.DesktopLyric.IsAutoFade)
            DesktopLyricsService.TryFadeIn();
        if (isUserRequested && ConfigManager.SystemConfig.KeepSystemAwake)
            Dispatcher.UIThread.Post(
                () => _ = Inhibitor.InhibitAsync(
                                       ConfigManager.SystemConfig.KeepDisplay,
                                       $"正在播放{CurrentMusicItem.Model.Title}")
                                   .ContinueWith(LoggerService.HandleException)
                                   .ConfigureAwait(true),
                DispatcherPriority.Background);
        AudioPlayer.Play();
        UpdateLyricsTimer();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Stop() {
        _ = SetMusicAsync(PlaylistItemModel.RefDefault, false, false)
            .ContinueWith(LoggerService.HandleException)
            .ConfigureAwait(false);
        Pause(true);
        AudioPlayer.Stop();
    }

    [RelayCommand]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ToggleMute() { IsMuted = !IsMuted; }

    [RelayCommand]
    public void Replay() {
        if (!VerifyMusicItem(CurrentMusicItem))
            return;

        Position = 0;
        OnPlayingChanged(true, true);
    }

    public void CheckForRemovedItems(IEnumerable<MusicItemModel> successItems) {
        if (!successItems.Contains(CurrentMusicItem.Model))
            return;

        if (IsPlaying) {
            IsPlaying = false;
            AudioPlayer.Stop();
        }

        NotificationService.Info($"当前音乐《{CurrentMusicItem.Model.Title}》被移除了哦~");
        NextMusicAsync(true).ContinueWith(LoggerService.HandleException).ConfigureAwait(false);
    }

    #endregion

    #region 辅助方法

    private void OnPlayingChanged(bool isPlayNow, bool isUserRequested) {
        IsPlaying = isPlayNow;
        SystemMediaControl.Status = isPlayNow ?
            SystemMediaInterop.MediaPlaybackStatus.Playing :
            SystemMediaInterop.MediaPlaybackStatus.Paused;

        if (isPlayNow)
            Play(isUserRequested);
        else
            Pause(isUserRequested);
    }

    public static bool VerifyMusicItem(PlaylistItemModel? musicItem) {
        if (musicItem == PlaylistItemModel.RefDefault) {
            return false;
        }

        if (musicItem is not { } item || !File.Exists(item.Model.FilePath)) {
            NotificationService.Error($"当前音乐不存在，请切换音乐！\n无法找到音乐文件:  {musicItem?.Model.FilePath}");
            LoggerService.Error($"无法找到当前音乐：{musicItem?.Model.FilePath}");
            return false;
        }

        if (PlaylistManager.Instance.Count == 0) {
            PlaylistManager.Instance.Add(MusicItemsManager.All.MusicItems.Values);
            NotificationService.Info($"当前播放列表为空，已自动填充为全部音乐！共 {PlaylistManager.Instance.Count} 首~");
        }

        if (PlaylistManager.Instance.ActualPlaylist.Contains(item))
            return true;

        PlaylistManager.Instance.Add(item.Model);

        NotificationService.Info($"当前音乐《{item.Model.Title}》不在播放列表中，已自动添加到播放列表末尾~");

        return true;
    }

    private async Task SetAndPlayAsync(int index, bool isUserRequested) {
        if (index == -1) {
            IsPreviousEnabled = false;
            IsNextEnabled = false;
            await SetCurrentMusicItemAsync(PlaylistItemModel.RefDefault, true).ConfigureAwait(false);
            LyricsModel = new LyricsModel { Offset = 0, Lyrics = MusicItemModel.Default.Lyrics };
            return;
        }

        if (index < -1 || index >= PlaylistManager.Count)
            return;

        PlaylistItemModel musicItem = PlaylistManager.ActualPlaylist[index];

        if (VerifyMusicItem(musicItem)) {
            await SetCurrentMusicItemAsync(musicItem, PlayerConfig.IsRestartPlay).ConfigureAwait(false);
            Play(isUserRequested);
        } else {
            await SetAndPlayAsync(
                    GetMusicItemIndex(
                        index,
                        (index - PlaylistManager.CurrentIndex) switch {
                            0    => 1,
                            1    => 1,
                            < -1 => 1,
                            -1   => -1,
                            > 1  => -1
                        }),
                    isUserRequested)
                .ConfigureAwait(false);
        }
    }

    [RelayCommand]
    private void TogglePlayMode() {
        // 循环切换播放模式
        PlayerConfig.PlayMode = (PlayMode)(((int)PlayerConfig.PlayMode + 1) % Enum.GetValues<PlayMode>().Length);
        PlaylistManager.PlayMode = PlayerConfig.PlayMode;
        UpdateSequenceControlStatus();
        LoggerService.Info($"切换到{PlayModeName}模式。");
        OnPropertyChanged(nameof(PlayModeName));
    }

    private int GetMusicItemIndex(int current, int offset) {
        //Count=0时返回-1; 否则返回最后一项
        int bound = PlaylistManager.Instance.Count - 1;
        current += offset;
        // current的边界判断
        // current越上界时，返回最后一项。
        if (bound <= 0 || current < 0)
            return bound;

        // 此处复用 bound 作为下界。       
        // current越下界时意味着歌单播放完毕。模式为顺序播放时返回-1，其它时返回首项。
        if (current <= bound)
            return current;

        if (PlaylistManager.Instance.PlayMode != PlayMode.Sequential)
            return 0;

        NotificationService.Info("顺序播放结束了哦~");
        return -1;
    }

    public string PlayModeName => I18NService.Lang.Translation[PlayerConfig.PlayMode.ToString(), nameof(PlayMode)];

    private void RegisterSystemMediaControlHandlers() {
        SystemMediaControl.IsPlayEnabled = true;
        SystemMediaControl.IsPauseEnabled = true;
        SystemMediaControl.IsStopEnabled = true;
        SystemMediaControl.PlayRequested += OnSystemPlayRequested;
        SystemMediaControl.PauseRequested += OnSystemPauseRequested;
        SystemMediaControl.NextRequested += OnSystemNextRequested;
        SystemMediaControl.PreviousRequested += OnSystemPreviousRequested;
        SystemMediaControl.StopRequested += OnSystemStopRequested;
        SystemMediaControl.SeekRequested += OnSystemSeekRequested;
    }

    private void UnregisterSystemMediaControlHandlers() {
        SystemMediaControl.PlayRequested -= OnSystemPlayRequested;
        SystemMediaControl.PauseRequested -= OnSystemPauseRequested;
        SystemMediaControl.NextRequested -= OnSystemNextRequested;
        SystemMediaControl.PreviousRequested -= OnSystemPreviousRequested;
        SystemMediaControl.StopRequested -= OnSystemStopRequested;
        SystemMediaControl.SeekRequested -= OnSystemSeekRequested;
    }

    private void OnSystemPlayRequested(object? sender, EventArgs e) {
        Dispatcher.UIThread.Post(() => {
            if (!IsPlaying)
                TogglePlayState();
        });
    }

    private void OnSystemPauseRequested(object? sender, EventArgs e) {
        Dispatcher.UIThread.Post(() => {
            if (IsPlaying)
                Pause(true);
        });
    }

    private void OnSystemNextRequested(object? sender, EventArgs e) { Dispatcher.UIThread.Post(NextMusic); }

    private void OnSystemPreviousRequested(object? sender, EventArgs e) { Dispatcher.UIThread.Post(PreviousMusic); }

    private void OnSystemStopRequested(object? sender, EventArgs e) { Dispatcher.UIThread.Post(Stop); }

    private void OnSystemSeekRequested(object? sender, PlaybackPositionChangedEventArgs e) {
        Dispatcher.UIThread.Post(() => Position = e.Position.TotalSeconds);
    }

    #endregion

    public async ValueTask DisposeAsync() {
        PlaybackStateChanged = null;
        MusicItemChanged = null;
        UnregisterSystemMediaControlHandlers();
        UnregisterHotkeyFunctions();
        AudioPlayer.Stop();
        PlayerConfig.LastPlayedFilePath = CurrentMusicItem.Model.FilePath;
        _lrcTimer.Elapsed -= OnLrcTimerElapsed;
        _lrcTimer.Dispose();
        await _audioPreprocessor.UpdateMusicPlayProgressAsync(CurrentMusicItem.Model).ConfigureAwait(false);
        await SavePlaylist().ConfigureAwait(false);
        GC.SuppressFinalize(this);
        return;

        async Task SavePlaylist() {
            IEnumerable<string> paths = PlaylistManager.Instance.SequentialPlaylist.Select(item => item.Model.FilePath);
            if (PlayerConfig.PlayMode == PlayMode.Random) {
                AvaloniaList<PlaylistItemModel> shuffled = PlaylistManager.Instance.ActualPlaylist;
                IEnumerable<int> orders = PlaylistManager.Instance.SequentialPlaylist.Select(shuffled.IndexOf);

                await PlaylistRepository.WriteAsync(paths, orders).ConfigureAwait(false);
            } else {
                await PlaylistRepository.WriteAsync(paths).ConfigureAwait(false);
            }

            AudioPlayer.Dispose();
            SystemMediaControl.Dispose();
        }
    }

    ~AudioPlayManager() { DisposeAsync().ConfigureAwait(false).GetAwaiter().GetResult(); }
}