using System;
using System.Collections.Generic;
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
using QwQ_Music.Utilities;
using QwQ_Music.Utilities.MessageBus;
using QwQ_Music.Views.UserControls;
using SoundFlow.Backends.MiniAudio;
using Ursa.Controls;
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

        _audioPlay.PositionChanged += OnPositionChanged;
        _audioPlay.PlaybackCompleted += AudioPlayOnPlaybackCompleted;
        StrongMessageBus.Instance.Subscribe<ExitReminderMessage>(ExitReminderMessageChanged);
    }

    #endregion

    #region 属性和字段

    private MiniAudioEngine _audioEngine = new(PlayerConfig.SampleRate);

    private readonly AudioPlay _audioPlay = new();

    [ObservableProperty]
    public partial MusicItemModel CurrentMusicItem { get; set; } = new("听你想听~", "YOU");

    [ObservableProperty]
    public partial bool IsPlaying { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<MusicItemModel> MusicItems { get; set; } = [];

    public static PlayerConfig PlayerConfig { get; } = ConfigInfoModel.PlayerConfig;

    [ObservableProperty]
    public partial LyricsModel LyricsModel { get; private set; } = new(new LyricsData());

    [ObservableProperty]
    public partial PlaylistModel Playlist { get; set; } = new(PlayerConfig.LatestPlayListName);

    public double CurrentDurationInSeconds
    {
        get;
        set
        {
            if (_isSlideCutting || !SetProperty(ref field, value))
                return;

            CurrentMusicItem.Current = TimeSpan.FromSeconds(field);
            LyricsModel.UpdateLyricsIndex(field);
            if (_isAutoChange)
                return;

            _audioPlay.Seek(value);
        }
    }

    private bool _isAutoChange;

    private bool _isSlideCutting;

    private int CurrentIndex => Playlist.MusicItems.IndexOf(CurrentMusicItem);

    public int Volume
    {
        get => PlayerConfig.Volume;
        set
        {
            if (value is > 100 or < 0)
                return;

            PlayerConfig.Volume = value;
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

    public float Speed
    {
        get => PlayerConfig.PlaybackSpeed;
        set
        {
            if (value < 0.5 || value > 1.5)
                return;

            PlayerConfig.PlaybackSpeed = value;
            OnPropertyChanged();
            _audioPlay.Speed = value;
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
            // 先加载所有音乐项到 MusicItems 集合
            await foreach (var item in DataBaseService.LoadDataAsync())
            {
                MusicItems.Add(MusicItemModel.FromDictionary(item));
            }

            // 加载播放列表并获取文件路径列表
            var filePaths = await Playlist.LoadAsync();

            // 根据文件路径从 MusicItems 中查找对应项目并添加到播放列表
            foreach (
                var musicItem in filePaths
                    .Select(filePath =>
                        MusicItems.FirstOrDefault(item => filePath != null && item.FilePath == filePath)
                    )
                    .OfType<MusicItemModel>()
                    .Where(musicItem => !Playlist.MusicItems.Contains(musicItem))
            )
            {
                Playlist.MusicItems.Add(musicItem);
            }

            if (Playlist.LatestPlayedMusic == null)
                return;

            var currentMusicItem = Playlist.MusicItems.FirstOrDefault(model =>
                model.FilePath == Playlist.LatestPlayedMusic
            );

            if (currentMusicItem != null)
                await SetCurrentMusicItem(currentMusicItem);
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

    partial void OnPlaylistChanged(PlaylistModel value)
    {
        PlaylistModel.ClearPlayedIndices(); // 当播放列表发生变化时，重置已播放索引列表
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
        Save();
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
    private void RemoveInMusicList(MusicItemModel musicItem) => Playlist.MusicItems.Remove(musicItem);

    [RelayCommand]
    private void ClearMusicItemCurrentDuration(MusicItemModel musicItem)
    {
        if (musicItem.Equals(CurrentMusicItem))
            CurrentDurationInSeconds = 0;
        else
            musicItem.Current = TimeSpan.Zero;
    }

    #endregion

    #region 音乐信息展示

    [RelayCommand]
    private static async Task ShowDialog(MusicItemModel musicItem)
    {
        var options = new OverlayDialogOptions
        {
            Title = "详细信息",
            CanLightDismiss = true,
            CanDragMove = true,
            IsCloseButtonVisible = true,
            CanResize = false,
        };

        await OverlayDialog.ShowCustomModal<AudioDetailedInfo, AudioDetailedInfoViewModel, object>(
            new AudioDetailedInfoViewModel(musicItem, await musicItem.GetExtensionsInfo()),
            options: options
        );
    }

    [RelayCommand]
    private static void OpenInExplorer(MusicItemModel musicItem)
    {
        if (string.IsNullOrEmpty(musicItem.FilePath) || !File.Exists(musicItem.FilePath))
        {
            Log.Warning("无法打开文件位置：文件不存在");
            return;
        }

        PathEnsurer.OpenInExplorer(musicItem.FilePath);
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
            _ => PlayerConfig is { PlayMode: PlayMode.Random, IsRealRandom: true }
                ? PlaylistModel.GetNonRepeatingRandomIndex(current, Playlist.Count)
            : isNext ? (current + 1) % Playlist.Count
            : (current - 1 + Playlist.Count) % Playlist.Count,
        };

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

    public static string PlayModeName =>
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


    private async void Save()
    {
        try
        {
            foreach (var item in MusicItems)
            {
                if (!item.IsModified || item.IsError)
                    continue;
                SaveMusicItemAsync(item).Wait();
            } // 保存音乐项

            await SavePlaylistAsync(); // 保存播放列表
        }
        catch (Exception e)
        {
            Log.Error($"数据库保存失败 : {e.Message}");
        }
    }

    private static async Task SaveMusicItemAsync(MusicItemModel item)
    {
        try
        {
            var data = item.Dump();
            string filePath = item.FilePath;

            // 检查记录是否存在，决定是更新还是插入
            bool exists = await DataBaseService.RecordExistsAsync(
                DataBaseService.Table.MUSICS,
                nameof(MusicItemModel.FilePath),
                filePath
            );

            if (exists)
            {
                // 更新现有记录
                await DataBaseService.UpdateDataAsync(
                    data,
                    DataBaseService.Table.MUSICS,
                    nameof(MusicItemModel.FilePath),
                    filePath
                );
            }
            else
            {
                // 插入新记录
                await DataBaseService.InsertDataAsync(data, DataBaseService.Table.MUSICS);
            }
        }
        catch (Exception ex)
        {
            Log.Error($"保存音乐项失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 保存当前播放列表到数据库
    /// </summary>
    private async Task SavePlaylistAsync()
    {
        // 如果为未命名（临时）播放列表，则不保存
        if (Playlist.Name == string.Empty)
            return;

        try
        {
            // 首先删除当前播放列表的所有记录
            await DataBaseService.DeleteDataAsync(
                DataBaseService.Table.PLAYLISTS,
                nameof(PlaylistModel.Name),
                Playlist.Name
            );

            // 保存播放列表中的每首歌曲
            foreach (var musicItem in Playlist.MusicItems)
            {
                var playlistData = new Dictionary<string, string?>
                {
                    [nameof(PlaylistModel.Name)] = Playlist.Name,
                    [nameof(MusicItemModel.FilePath)] = musicItem.FilePath,
                };

                await DataBaseService.InsertDataAsync(playlistData, DataBaseService.Table.PLAYLISTS);
            }

            // 更新或插入播放列表名称记录
            var listNameData = Playlist.Dump();

            bool exists = await DataBaseService.RecordExistsAsync(
                DataBaseService.Table.LISTNAMES,
                nameof(PlaylistModel.Name),
                Playlist.Name
            );

            if (exists)
            {
                await DataBaseService.UpdateDataAsync(
                    listNameData,
                    DataBaseService.Table.LISTNAMES,
                    nameof(PlaylistModel.Name),
                    Playlist.Name
                );
            }
            else
            {
                await DataBaseService.InsertDataAsync(listNameData, DataBaseService.Table.LISTNAMES);
            }
        }
        catch (Exception ex)
        {
            Log.Error($"保存播放列表失败: {ex.Message}");
        }
    }

    #endregion

    #region 音频处理

    public async Task SetCurrentMusicItem(MusicItemModel musicItem, bool restart = false)
    {
        if (!Playlist.MusicItems.Contains(musicItem))
        {
            Playlist = new PlaylistModel { MusicItems = new ObservableCollection<MusicItemModel>(MusicItems) };
        }

        if (restart || IsNearEnd(musicItem))
        {
            musicItem.Current = TimeSpan.Zero;
        }

        _audioPlay.Stop();

        try
        {
            var args = new CurrentMusicItemChangedCancelEventArgs(musicItem);
            CurrentMusicItemChanging?.Invoke(this, args);
            if (args.Cancel)
                return;
            await InitializeAudioTrackAsync(musicItem);
            _isSlideCutting = true;
            CurrentMusicItem = musicItem;
            _isSlideCutting = false;

            LyricsModel = new LyricsModel(await musicItem.Lyrics);

            CurrentDurationInSeconds = musicItem.Current.TotalSeconds;
            CurrentMusicItemChanged?.Invoke(this, musicItem);
            Playlist.LatestPlayedMusic = CurrentMusicItem.FilePath;
        }
        catch (Exception ex)
        {
            Log.Error($"初始化音轨失败: {ex.Message}");
        }
    }

    private async Task InitializeAudioTrackAsync(MusicItemModel musicItem)
    {
        await Task.Run(async () =>
        {
            // 如果采样率匹配且增益值已设置，直接初始化音频并返回
            if (
                musicItem.Gain > 0f
                && !PlayerConfig.IsAutoSetSampleRate
                && PlayerConfig.SampleRate == _audioEngine.SampleRate
            )
            {
                _audioPlay.InitializeAudio(musicItem.FilePath, musicItem.Gain);
                return;
            }

            // 获取音频扩展信息
            var ex = await musicItem.GetExtensionsInfo();

            // 处理采样率
            int targetSampleRate = PlayerConfig.IsAutoSetSampleRate ? ex.SamplingRate : PlayerConfig.SampleRate;

            if (targetSampleRate != _audioEngine.SampleRate)
            {
                await SetOutputSampleRate(targetSampleRate);
            }

            // 处理增益值
            if (musicItem.Gain <= 0f)
            {
                musicItem.Gain = AudioHelper.CalcGainOfMusicItem(musicItem.FilePath, ex.SamplingRate, ex.Channels);
            }

            // 初始化音频
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
