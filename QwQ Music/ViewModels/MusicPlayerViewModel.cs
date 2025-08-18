using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using CommunityToolkit.Mvvm.Input;
using NcmdumpCSharp.Core;
using QwQ_Music.Common.Audio;
using QwQ_Music.Common.Interfaces;
using QwQ_Music.Common.Manager;
using QwQ_Music.Common.Services;
using QwQ_Music.Models;
using QwQ_Music.Models.ConfigModels;
using QwQ_Music.Models.Enums;
using QwQ_Music.ViewModels.Bases;
using Log = QwQ_Music.Common.Services.LoggerService;

namespace QwQ_Music.ViewModels;

public partial class MusicPlayerViewModel : ViewModelBase, IMusicPlayer
{
    private MusicPlayerViewModel()
    {
        InitializeAsync();

        _audioPlay.PositionChanged += OnPositionChanged;
        _audioPlay.PlaybackCompleted += AudioPlayOnPlaybackCompleted;

        // 初始化歌词滚动定时器
        _lyricsTimer = new Timer();
        _lyricsTimer.Elapsed += OnLyricsTimerElapsed;
        _lyricsTimer.AutoReset = false;

        // 注册热键功能
        RegisterHotkeyFunctions();
    }

    public static MusicPlayerViewModel Default { get; } = new();

    /// <summary>
    ///     注册热键功能
    /// </summary>
    private void RegisterHotkeyFunctions()
    {
        HotkeyService.RegisterFunctionAction(HotkeyFunction.PreviousSong, () => PreviousSongCommand.Execute(null));

        HotkeyService.RegisterFunctionAction(HotkeyFunction.NextSong, () => NextSongCommand.Execute(null));

        HotkeyService.RegisterFunctionAction(HotkeyFunction.PlayPause, () => TogglePlayStaceCommand.Execute(null));

        HotkeyService.RegisterFunctionAction(HotkeyFunction.ToggleMute, () => ToggleMuteCommand.Execute(null));

        HotkeyService.RegisterFunctionAction(HotkeyFunction.TogglePlayMode, () => TogglePlayModeCommand.Execute(null));

        HotkeyService.RegisterFunctionAction(
            HotkeyFunction.VolumeUp, () =>
            {
                if (Volume < 100)
                    Volume += 5;
            }
        );

        HotkeyService.RegisterFunctionAction(
            HotkeyFunction.VolumeDown, () =>
            {
                if (Volume > 0)
                    Volume -= 5;
            }
        );

        HotkeyService.RegisterFunctionAction(
            HotkeyFunction.RefreshCurrentMusic,
            () => RefreshPlaybackCommand.Execute(null)
        );

        HotkeyService.RegisterFunctionAction(
            HotkeyFunction.ShowPlaylistInfo, () =>
            {
                NotificationService.Info("你知道吗？",
                    $"当前播放列表有: {MusicPlayListManager.Count} 首音乐！\n" +
                    $"现在正在播放第 {MusicPlayListManager.CurrentIndex} 首");
            }
        );

        HotkeyService.RegisterFunctionAction(
            HotkeyFunction.ShowCurrentInfo, () =>
            {
                NotificationService.Info("你知道吗？",
                    $"{(IsPlaying ? "正在播放" : "已暂停")}的音乐叫做: {CurrentMusicItem.Title} 哦！\n" +
                    $"你的音量是: {Volume}% "
                );
            }
        );
    }

    #region 属性和字段

    private readonly AudioPlay _audioPlay = new();
    private readonly Timer _lyricsTimer;

    public static MusicItemManager MusicItemManager => MusicItemManager.Default;

    public static MusicPlayListManager MusicPlayListManager => MusicPlayListManager.Default;

    public static MusicItemModel CurrentMusicItem
    {
        get => MusicPlayListManager.CurrentMusicItem;
        set => MusicPlayListManager.CurrentMusicItem = value;
    }

    public bool IsPlaying
    {
        get;
        set
        {
            if (!SetProperty(ref field, value))
                return;

            PlaybackStateChanged?.Invoke(this, value);
        }
    }

    public PlayerConfig PlayerConfig { get; } = ConfigManager.PlayerConfig;

    public LyricsModel LyricsModel { get; } = new();

    public double Position
    {
        get => CurrentMusicItem.Current.TotalSeconds;
        set => Seek(value);
    }

    public int Volume
    {
        get => PlayerConfig.Volume;
        set
        {
            int result = Math.Clamp(value, 0, 100);

            if (result == PlayerConfig.Volume)
                return;

            PlayerConfig.Volume = result;
            _audioPlay.Volume = result / 100f;

            IsMuted = result == 0f;
            OnPropertyChanged();
        }
    }

    public bool IsMuted
    {
        get => PlayerConfig.IsMuted;
        set
        {
            if (value == PlayerConfig.IsMuted)
                return;

            PlayerConfig.IsMuted = value;
            _audioPlay.IsMute = value;

            OnPropertyChanged();
        }
    }

    public float Speed
    {
        get => PlayerConfig.PlaybackSpeed;
        set
        {
            float result = Math.Clamp(value, 0.5f, 1.5f);

            if (Math.Abs(PlayerConfig.PlaybackSpeed - result) < 1e-6f)
                return;

            PlayerConfig.PlaybackSpeed = result;
            OnPropertyChanged();
            _audioPlay.Speed = result;
        }
    }

    public int LyricOffset
    {
        get => LyricsModel.LyricOffset;
        set
        {
            LyricsModel.LyricOffset = value;
            CurrentMusicItem.LyricOffset = value;

            OnPropertyChanged();
        }
    }

    #endregion

    #region 事件

    public event EventHandler<bool>? PlaybackStateChanged;

    public event EventHandler<MusicItemModel>? PlayerItemChanged;

    #endregion

    #region 初始化方法

    private async void InitializeAsync()
    {
        try
        {
            await MusicItemManager.Initialize();
            await MusicPlayListManager.Initialize();
            
            if (PlayerConfig.LastPlayedFilePath == null)
                return;

            foreach (var item in MusicPlayListManager.PlayList.Where(item => item.FilePath == PlayerConfig.LastPlayedFilePath))
            {
                await SetCurrentMusicItem(item);
            }
        }
        catch (Exception e)
        {
            await Log.ErrorAsync($"初始化播放器模型出错！\n{e.Message}").ConfigureAwait(false);
        }
    }

    public void Shutdown()
    {
        _audioPlay.PositionChanged -= OnPositionChanged;
        _audioPlay.PlaybackCompleted -= AudioPlayOnPlaybackCompleted;
        _lyricsTimer.Elapsed -= OnLyricsTimerElapsed;
        _lyricsTimer.Dispose();

        _audioPlay.Dispose();

        SaveFinalState();
    }

    private void SaveFinalState()
    {
        PlayerConfig.LastPlayedFilePath = CurrentMusicItem.FilePath;

        if (CurrentMusicItem.Current != _initialTime)
        {
            MusicItemManager.UpdatePlayProgress(CurrentMusicItem.FilePath, CurrentMusicItem.Current);
        }
    }

    #endregion

    #region 事件处理

    private void OnPositionChanged(object? sender, double positionInSeconds)
    {
        CurrentMusicItem.Current = TimeSpan.FromSeconds(positionInSeconds);
        OnPropertyChanged(nameof(Position));
    }

    private void UpdateLyricsTimer()
    {
        if (!IsPlaying)
            return;

        // 停止当前定时器
        _lyricsTimer.Stop();

        // 计算到下一句歌词的时间间隔
        double nextInterval = LyricsModel.GetNextLyricsInterval(_audioPlay.Position);

        if (!(nextInterval > 0))
            return;

        // 设置定时器间隔为到下一句歌词的时间
        _lyricsTimer.Interval = nextInterval * 1000; // 转换为毫秒
        _lyricsTimer.Start();
    }

    private void OnLyricsTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        // 更新当前歌词
        LyricsModel.UpdateLyricsIndex(_audioPlay.Position);

        // 计算到下一句歌词的时间间隔
        double nextInterval = LyricsModel.GetNextLyricsInterval(_audioPlay.Position);

        if (!(nextInterval > 0))
            return;

        // 设置定时器间隔为到下一句歌词的时间
        _lyricsTimer.Interval = nextInterval * 1000; // 转换为毫秒
        _lyricsTimer.Start();
    }

    private async void AudioPlayOnPlaybackCompleted(object? sender, EventArgs e)
    {
        try
        {
            CurrentMusicItem.Current = TimeSpan.Zero;

            // 根据播放模式处理播放完成后的行为
            if (PlayerConfig.PlayMode == PlayMode.SingleLoop)
            {
                RefreshPlayback(); // 单曲循环模式下，重新播放当前歌曲
            }
            else if (PlayerConfig.AutoSwitchNext)
            {
                await NextSong();
            }
            else
            {
                OnPlayingChanged(false);
            }
        }
        catch (Exception ex)
        {
            NotificationService.Error($"音频播放完成后切换下一首音频时遇到错误：{ex.Message}");
            await Log.ErrorAsync($"音频播放完成后切换下一首音频时遇到错误：{ex.Message}\n{ex.StackTrace}");
        }
    }

    #endregion

    #region 播放控制方法

    [RelayCommand]
    public async Task TogglePlayStace()
    {
        if (MusicItemManager.Count == 0)
        {
            NotificationService.Info("音乐库中一首音乐也没有啦，需要我为你播放一首空空如也吗？");

            return;
        }

        if (CurrentMusicItem == MusicPlayListManager.DefaultMusicItem)
        {
            CurrentMusicItem = MusicItemManager.MusicItems.First();
            await SetCurrentMusicItem(CurrentMusicItem);
        }

        if (VerifyMusicItem(CurrentMusicItem))
        {
            OnPlayingChanged(!IsPlaying);
        }
    }

    [RelayCommand]
    public async Task PlayThisMusic(MusicItemModel musicItem)
    {
        if (!VerifyMusicItem(musicItem))
        {
            return;
        }

        if (CurrentMusicItem.Equals(musicItem))
        {
            OnPlayingChanged(!IsPlaying);
        }
        else
        {
            await SetCurrentMusicItem(musicItem, true);
            OnPlayingChanged(true);
        }
    }

    public void Seek(double currentPosition)
    {
        _audioPlay.Seek(currentPosition);
        LyricsModel.UpdateLyricsIndex(currentPosition);
        OnPropertyChanged(nameof(Position));

        // 当播放位置改变时，重新设置歌词定时器
        if (IsPlaying)
        {
            UpdateLyricsTimer();
        }
    }

    public async Task ToggleMusicAsync(MusicItemModel musicItem)
    {
        if (VerifyMusicItem(musicItem))
        {
            OnPlayingChanged(false);
            await SetCurrentMusicItem(musicItem);
        }
    }

    [RelayCommand]
    public async Task PreviousSong()
    {
        await SetAndPlay(GetMusicItemIndex(MusicPlayListManager.CurrentIndex, false));
    }

    [RelayCommand]
    public async Task NextSong()
    {
        await SetAndPlay(GetMusicItemIndex(MusicPlayListManager.CurrentIndex));
    }

    [RelayCommand]
    public void ToggleMute()
    {
        IsMuted = !IsMuted;
    }

    [RelayCommand]
    public void RefreshPlayback()
    {
        if (!VerifyMusicItem(CurrentMusicItem))
            return;

        Seek(0);
        OnPlayingChanged(true);
    }

    [RelayCommand]
    public void ClearPlayDuration(MusicItemModel musicItem)
    {
        if (musicItem.Equals(CurrentMusicItem))
            Seek(0);
        else
            musicItem.Current = TimeSpan.Zero;
    }

    #endregion

    #region 辅助方法

    private void OnPlayingChanged(bool value)
    {
        IsPlaying = value;

        if (value)
        {
            _audioPlay.Play();
            UpdateLyricsTimer();
        }
        else
        {
            _audioPlay.Pause();
            _lyricsTimer.Stop();
        }
    }

    private static bool VerifyMusicItem(MusicItemModel musicItem)
    {
        if (!File.Exists(musicItem.FilePath))
        {
            NotificationService.Error($"当前音乐不存在，请切换音乐！\n无法找到音乐文件:  {musicItem.FilePath}");

            return false;
        }

        if (MusicPlayListManager.Count == 0)
        {
            MusicPlayListManager.AddRange(MusicItemManager.MusicItems);
            NotificationService.Info($"当前播放列表为空，已自动填充为全部音乐！共 {MusicPlayListManager.Count} 首~");
        }

        if (MusicPlayListManager.PlayList.Contains(musicItem))
            return true;

        MusicPlayListManager.Add(musicItem);

        NotificationService.Info($"当前音乐《{musicItem.Title}》不在播放列表中，以自动添加到播放列表末尾~");

        return true;
    }

    private async Task SetAndPlay(int index)
    {
        if (index < 0 || index >= MusicPlayListManager.Count)
            return;

        var musicItem = MusicPlayListManager.PlayList[index];

        if (VerifyMusicItem(musicItem))
        {
            await SetCurrentMusicItem(musicItem, PlayerConfig.IsRestartPlay);
            OnPlayingChanged(true);
        }
    }

    [RelayCommand]
    private void TogglePlayMode()
    {
        // 循环切换播放模式
        var previousMode = PlayerConfig.PlayMode;
        PlayerConfig.PlayMode = (PlayMode)(((int)PlayerConfig.PlayMode + 1) % 3);

        // 如果切换到随机播放模式，则打乱播放列表
        if (previousMode != PlayMode.Random && PlayerConfig is { PlayMode: PlayMode.Random, IsRealRandom: false })
        {
            MusicPlayListManager.Shuffle();
        }

        OnPropertyChanged(nameof(PlayModeName));
    }

    private int GetMusicItemIndex(int current, bool isNext = true)
    {
        return MusicPlayListManager.Count switch
        {
            <= 0 => -1,
            1 => 0,
            _ => PlayerConfig is { PlayMode: PlayMode.Random, IsRealRandom: true }
                ? PlayedIndicesService.GetNonRepeatingRandomIndex(current, MusicPlayListManager.Count)
                : isNext
                    ? (current + 1) % MusicPlayListManager.Count
                    : (current - 1 + MusicPlayListManager.Count) % MusicPlayListManager.Count,
        };
    }

    public string PlayModeName =>
        PlayerConfig.PlayMode switch
        {
            PlayMode.Sequential => "顺序播放",
            PlayMode.Random => "随机播放",
            PlayMode.SingleLoop => "单曲循环",
            _ => "未知模式",
        };

    private static bool IsNearEnd(MusicItemModel musicItem)
    {
        return Math.Abs(musicItem.Duration.TotalSeconds - musicItem.Current.TotalSeconds) < 5;
    }

    #endregion

    #region 音频处理

    private TimeSpan _initialTime = TimeSpan.Zero;

    private async Task SetCurrentMusicItem(MusicItemModel musicItem, bool restart = false)
    {
        if (restart || IsNearEnd(musicItem))
        {
            musicItem.Current = TimeSpan.Zero;
        }

        try
        {
            // 根据文件类型初始化音频
            string extension = Path.GetExtension(musicItem.FilePath).ToUpper();
            
            // 预处理，判断音频格式
            AudioPreprocessor.UpdateAudioFormat(_audioPlay,musicItem);
            
            if (extension == AudioFileValidator.AudioFormatsExtendToNameMap[AudioFileValidator.ExtendAudioFormats.Ncm])
            {
                await InitializeNcmAudioTrackAsync(musicItem);
            }
            else
            {
                await Task.Run(() => _audioPlay.InitializeAudio(musicItem.FilePath, musicItem.Gain));
            }

            if (CurrentMusicItem.Current != _initialTime)
            {
                _ = Task.Run(() => { MusicItemManager.UpdatePlayProgress(CurrentMusicItem.FilePath, CurrentMusicItem.Current); });
            }

            Position = 0;

            CurrentMusicItem = musicItem;
            OnPropertyChanged(nameof(CurrentMusicItem));

            _initialTime = musicItem.Current;
            LyricOffset = musicItem.LyricOffset;
            LyricsModel.UpdateLyricsData(await MusicExtractor.ExtractMusicLyricsAsync(musicItem.FilePath));
            Seek(musicItem.Current.TotalSeconds);

            PlayerItemChanged?.Invoke(this, musicItem);
        }
        catch (Exception ex)
        {
            await Log.ErrorAsync($"初始化新音轨失败:\n{ex.Message}\n{ex.StackTrace}");

            NotificationService.Error("播放失败", $"初始化新音轨失败: {ex.Message}\n可能的原因: 当前{musicItem.EncodingFormat}格式不支持解码");
        }
    }

    private async Task InitializeNcmAudioTrackAsync(MusicItemModel musicItem)
    {
        try
        {
            using var crypt = new NeteaseCrypt(musicItem.FilePath);
            var (audioStream, _) = await crypt.DumpToStreamAsync();

            if (audioStream != null)
            {
                // 对于NCM，我们暂时不处理ReplayGain
                _audioPlay.InitializeAudio(audioStream, 0);
            }
        }
        catch (Exception e)
        {
            IsPlaying = false;
            await Log.ErrorAsync($"初始化NCM音轨失败: {musicItem.FilePath}\n{e.Message}");

            NotificationService.Error("播放失败", $"初始化NCM音轨失败: {musicItem.FilePath}");
        }
    }

    #endregion
}
