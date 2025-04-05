using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Enums;
using QwQ_Music.Models;
using QwQ_Music.Models.ConfigModel;
using QwQ_Music.Services;
using QwQ_Music.Services.Audio;
using QwQ_Music.Services.ConfigIO;
using QwQ_Music.Utilities.MessageBus;
using SoundFlow.Backends.MiniAudio;
using Log = QwQ_Music.Services.LoggerService;

namespace QwQ_Music.ViewModels;

public partial class MusicPlayerViewModel : ViewModelBase
{
    #region 单例实现

    // ReSharper disable once InconsistentNaming
    private static readonly Lazy<MusicPlayerViewModel> _instance = new(() => new MusicPlayerViewModel());
    public static MusicPlayerViewModel Instance => _instance.Value;

    private MusicPlayerViewModel()
    {
        InitializeAsync();

        _audioPlay.Volume = Volume;
        _audioPlay.IsMute = IsMuted;
        _audioPlay.PositionChanged += OnPositionChanged;
        _audioPlay.PlaybackCompleted += AudioPlayOnPlaybackCompleted;
        StrongMessageBus.Instance.Subscribe<ExitReminderMessage>(ExitReminderMessageChanged);
    }

    #endregion

    #region 属性和字段

    private MiniAudioEngine _audioEngine = new();

    private readonly AudioPlay _audioPlay = new();

    [ObservableProperty]
    public partial double CurrentDurationInSeconds { get; set; }

    [ObservableProperty]
    public partial MusicItemModel CurrentMusicItem { get; set; } = new("听你想听~", "YOU");

    [ObservableProperty]
    public partial bool IsPlaying { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<MusicItemModel> MusicItems { get; set; } = [];

    public PlayerConfig PlayerConfig { get; } = ConfigInfoModel.PlayerConfig;

    public static PlaylistModel Playlist { get; } = new(PlayerConfig.LatestPlayListName);

    private bool _isAutoChange;


    private int CurrentIndex => Playlist.MusicItems.IndexOf(CurrentMusicItem);

    public int Volume
    {
        get => PlayerConfig.Volume;
        set
        {
            PlayerConfig.Volume = Math.Clamp(value, 0, 100);
            OnPropertyChanged();
            _audioPlay.Volume = value;
            IsMuted = value == 0f;
        }
    }

    public bool IsMuted
    {
        get => PlayerConfig.IsMuted;
        set
        {
            PlayerConfig.IsMuted = value;
            OnPropertyChanged();
            _audioPlay.IsMute = value;
        }
    }

    #endregion

    #region 事件

    public event EventHandler<bool>? PlaybackStateChanged;
    public event EventHandler<CurrentMusicItemChangedCancelEventArgs>? CurrentMusicItemChanging;
    public event EventHandler<MusicItemModel>? CurrentMusicItemChanged;
    public event EventHandler<ObservableCollection<MusicItemModel>>? MusicItemsChanged;

    #endregion

    #region 初始化方法

    private void InitializeAsync()
    {
        InitializeMusicItemAsync().ConfigureAwait(false);
    }

    private async Task InitializeMusicItemAsync()
    {
        try
        {
            /*var wait = Playlist.LoadAsync();*/

            await foreach (var item in DataBaseService.LoadFromDatabaseAsync<MusicItemModel>())
            {
                MusicItems.Add(item);
            }

            /*await wait;
            var currentMusicItem = Playlist.MusicItems.FirstOrDefault(model =>
                model.FilePath == Playlist.LatestPlayedMusic
            );
            if (currentMusicItem != null)
                await SetCurrentMusicItem(currentMusicItem);*/
        }
        catch (Exception ex)
        {
            Log.Error($"Unexpected error occurred while initializing music playlist: {ex.Message}");
        }
    }

    #endregion

    #region 属性变更处理


    partial void OnIsPlayingChanged(bool value)
    {
        PlaybackStateChanged?.Invoke(this, value);
    }

    partial void OnMusicItemsChanged(ObservableCollection<MusicItemModel> value)
    {
        MusicItemsChanged?.Invoke(this, value);
    }

    partial void OnCurrentDurationInSecondsChanged(double value)
    {
        CurrentMusicItem.Current = TimeSpan.FromSeconds(value);
        if (_isAutoChange)
            return;

        _audioPlay.Seek(value);
    }

    #endregion

    #region 事件处理

    private void OnPositionChanged(object? sender, double positionInSeconds)
    {
        _isAutoChange = true;
        CurrentDurationInSeconds = positionInSeconds;
        _isAutoChange = false;
    }

    private async void AudioPlayOnPlaybackCompleted(object? sender, EventArgs e)
    {
        try
        {
            CurrentMusicItem.Current = TimeSpan.Zero;

            // 根据播放模式处理播放完成后的行为
            if (PlayerConfig.PlayMode == PlayMode.SingleLoop)
            {
                // 单曲循环模式下，重新播放当前歌曲
                await RefreshCurrentMusicItem();
            }
            else if (PlayerConfig.AutoSwitchNext)
            {
                await ToggleNextSong();
            }
            else
            {
                OnPlayingChanged(false);
            }
            CurrentDurationInSeconds = 0;
        }
        catch (Exception ex)
        {
            Log.Error($"音频播放完成后切换下一首音频时遇到错误：{ex.Message}");
        }
    }

    private void ExitReminderMessageChanged(ExitReminderMessage message)
    {
        _audioPlay.PositionChanged -= OnPositionChanged;
        _audioPlay.PlaybackCompleted -= AudioPlayOnPlaybackCompleted;

        _audioEngine.Dispose();
        _audioPlay.Dispose();
        SaveConfig();
    }

    #endregion

    #region 播放控制方法


    [RelayCommand]
    private async Task TogglePlayback()
    {
        if (await FallbackMusicItem())
            return;

        OnPlayingChanged(!IsPlaying);
    }

    [RelayCommand]
    private async Task PlaySpecifiedMusic(MusicItemModel? musicItem)
    {
        if (musicItem == null)
            return;

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

    [RelayCommand]
    private async Task TogglePreviousSong() => await SetAndPlay(GetMusicItemIndex(CurrentIndex, false));

    [RelayCommand]
    private async Task ToggleNextSong() => await SetAndPlay(GetMusicItemIndex(CurrentIndex));

    [RelayCommand]
    private void ToggleMuteMode() => IsMuted = !IsMuted;

    [RelayCommand]
    private async Task RefreshCurrentMusicItem()
    {
        await SetCurrentMusicItem(CurrentMusicItem, true);
        OnPlayingChanged(true);
    }

    #endregion

    #region 播放列表管理

    [RelayCommand]
    private void AddToMusicListNextItem(MusicItemModel musicItem)
    {
        RemoveInMusicList(musicItem);
        Playlist.MusicItems.Insert(CurrentIndex + 1, musicItem);
    }

    [RelayCommand]
    private static void RemoveInMusicList(MusicItemModel musicItem) => Playlist.MusicItems.Remove(musicItem);

    [RelayCommand]
    private void ClearMusicItemCurrentDuration(MusicItemModel musicItem)
    {
        if (musicItem.Equals(CurrentMusicItem))
            CurrentDurationInSeconds = 0;
        else
            musicItem.Current = TimeSpan.Zero;
    }

    #endregion

    #region 辅助方法

    private void OnPlayingChanged(bool value)
    {
        if (value)
            _audioPlay.Play();
        else
            _audioPlay.Pause();

        IsPlaying = value;
    }

    private async Task<bool> FallbackMusicItem()
    {
        if (File.Exists(CurrentMusicItem.FilePath))
            return false;

        if (Playlist.Count == 0)
            Playlist.MusicItems = new ObservableCollection<MusicItemModel>(MusicItems);

        var item = MusicItems.FirstOrDefault();
        if (item != null)
            await SetCurrentMusicItem(item);
        return item == null;
    }

    private async Task SetAndPlay(int index)
    {
        if (index < 0 || index >= Playlist.Count)
            return;
        await SetCurrentMusicItem(Playlist.MusicItems[index], PlayerConfig.IsRestartPlay);
        OnPlayingChanged(true);
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
            ShufflePlaylist();
        }

        OnPropertyChanged(nameof(PlayModeName));
    }

    private int GetMusicItemIndex(int current, bool isNext = true) =>
        Playlist.Count switch
        {
            <= 0 => -1,
            1 => 0,
            _ => PlayerConfig is { PlayMode: PlayMode.Random, IsRealRandom: true } ? RandomMusicItemIndex(current)
            : isNext ? (current + 1) % Playlist.Count
            : (current - 1 + Playlist.Count) % Playlist.Count,
        };

    private static int RandomMusicItemIndex(int current)
    {
        var random = new Random();
        int randomIndex;
        do
        {
            randomIndex = random.Next(0, Playlist.Count);
        } while (randomIndex == current && Playlist.Count > 1);

        return randomIndex;
    }

    private void ShufflePlaylist()
    {
        if (Playlist.Count <= 1)
            return;

        // 保存当前播放的歌曲
        var currentItem = CurrentMusicItem;

        // 创建临时列表并打乱
        var tempList = Playlist.MusicItems.ToList();
        var random = new Random();

        // Fisher-Yates 洗牌算法
        for (int i = tempList.Count - 1; i > 0; i--)
        {
            int j = random.Next(0, i + 1);
            (tempList[i], tempList[j]) = (tempList[j], tempList[i]);
        }

        // 如果当前有播放的歌曲，确保它在列表的当前位置
        if (Playlist.MusicItems.Contains(currentItem))
        {
            int currentIndex = CurrentIndex;
            tempList.Remove(currentItem);
            tempList.Insert(currentIndex, currentItem);
        }

        // 更新播放列表
        Playlist.MusicItems = new ObservableCollection<MusicItemModel>(tempList);
    }

    public string PlayModeName =>
        PlayerConfig.PlayMode switch
        {
            PlayMode.Sequential => "顺序播放",
            PlayMode.Random => "随机播放",
            PlayMode.SingleLoop => "单曲循环",
            _ => "未知模式",
        };

    private static bool IsNearEnd(MusicItemModel musicItem) =>
        Math.Abs(musicItem.Duration.TotalSeconds - musicItem.Current.TotalSeconds) < 0.5;

    #endregion

    #region 数据持久化

    private void SaveConfig()
    {
        foreach (var item in MusicItems)
        {
            DataBaseService.SaveToDatabaseAsync(item, DataBaseService.Table.MUSICS).ConfigureAwait(false);
        }
    }

    #endregion

    #region 音频处理

    public async Task SetCurrentMusicItem(MusicItemModel musicItem, bool restart = false)
    {
        if (!Playlist.MusicItems.Contains(musicItem))
        {
            Playlist.MusicItems = new ObservableCollection<MusicItemModel>(MusicItems);
        }

        if (restart || IsNearEnd(musicItem))
        {
            musicItem.Current = TimeSpan.Zero;
        }

        _audioPlay.Stop();

        if (PlayerConfig.SampleRate != _audioEngine.SampleRate)
        {
            await SetOutputSampleRate(PlayerConfig.SampleRate);
        }

        var args = new CurrentMusicItemChangedCancelEventArgs(musicItem);

        try
        {
            CurrentMusicItemChanging?.Invoke(this, args);
            if (args.Cancel)
                return;
            await InitializeAudioTrackAsync(musicItem);
            CurrentMusicItem = musicItem;
            CurrentDurationInSeconds = musicItem.Current.TotalSeconds;
            CurrentMusicItemChanged?.Invoke(this, musicItem);
        }
        catch (Exception ex)
        {
            Log.Error($"初始化音轨失败: {ex.Message}");
        }
    }

    private async Task InitializeAudioTrackAsync(MusicItemModel musicItem)
    {
        await Task.Run(() =>
        {
            if (musicItem.Gain <= 0f)
                AudioHelper.CalcGainOfMusicItem(musicItem);

            _audioPlay.InitializeAudio(musicItem.FilePath, musicItem.Gain);
        });
    }

    private async Task SetOutputSampleRate(int sampleRate)
    {
        try
        {
            await Task.Run(() =>
            {
                _audioEngine.Dispose();
                _audioEngine = new MiniAudioEngine(sampleRate);
            });
        }
        catch (Exception e)
        {
            Log.Error($"设置采样率时出现错误 : {e.Message}");
        }
    }

    #endregion
}

public class CurrentMusicItemChangedCancelEventArgs(MusicItemModel musicItem) : CancelEventArgs
{
    public MusicItemModel MusicItem { get; } = musicItem;
}
