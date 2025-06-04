using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Definitions;
using QwQ_Music.Definitions.Enums;
using QwQ_Music.Models;
using QwQ_Music.Models.ConfigModel;
using QwQ_Music.Services;
using QwQ_Music.Services.Audio;
using QwQ_Music.Services.ConfigIO;
using QwQ_Music.Utilities;
using QwQ_Music.ViewModels.Pages;
using QwQ_Music.ViewModels.UserControls;
using QwQ_Music.ViewModels.ViewModelBases;
using QwQ_Music.Views.UserControls;
using QwQ.Avalonia.Utilities.MessageBus;
using SoundFlow.Backends.MiniAudio;
using Ursa.Controls;
using Log = QwQ_Music.Services.LoggerService;
using Notification = Ursa.Controls.Notification;

namespace QwQ_Music.ViewModels;

public partial class MusicPlayerViewModel : ViewModelBase
{
    #region 单例实现

    private static readonly Lazy<MusicPlayerViewModel> _instance = new(() => new MusicPlayerViewModel());
    public static MusicPlayerViewModel Instance => _instance.Value;

    private MusicPlayerViewModel()
    {
        InitializeAsync();

        _audioPlay.PositionChanged += OnPositionChanged;
        _audioPlay.PlaybackCompleted += AudioPlayOnPlaybackCompleted;
        PlayList.MusicItems.CollectionChanged += MusicItemsOnCollectionChanged;
        MessageBus
            .ReceiveMessage<ExitReminderMessage>(this)
            .WithHandler(ExitReminderMessageHandler)
            .AsWeakReference()
            .Subscribe();
    }

    #endregion

    #region 属性和字段

    private MiniAudioEngine _audioEngine = new(PlayerConfig.SampleRate);

    private readonly AudioPlay _audioPlay = new();

    [ObservableProperty]
    public partial MusicItemModel CurrentMusicItem { get; set; } = new("听你想听~", "YOU");

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

    [ObservableProperty]
    public partial ObservableCollection<MusicItemModel> MusicItems { get; set; } = [];

    public static PlayerConfig PlayerConfig { get; } = ConfigInfoModel.PlayerConfig;

    public MusicListsPageViewModel MusicListsViewModel { get; } = new();

    [ObservableProperty]
    public partial LyricsModel LyricsModel { get; private set; } = new(new LyricsData());

    [ObservableProperty]
    public partial MusicListModel PlayList { get; set; } = new(string.Empty);

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

    private int CurrentIndex => PlayList.MusicItems.IndexOf(CurrentMusicItem);

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

    /// <summary>
    /// 获取播放位置（秒）
    /// </summary>
    public double Position => _audioPlay.Position;

    #endregion

    #region 事件
    public event EventHandler<bool>? PlaybackStateChanged;
    public event EventHandler<MusicItemModel>? CurrentMusicItemChanged;

    #endregion

    #region 初始化方法

    private async void InitializeAsync()
    {
        await InitializeMusicItemAsync(); // 加载播放列表
        await InitializePlaylistAsync();
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

            await MessageBus
                .CreateMessage(new LoadCompletedMessage(nameof(MusicItems)))
                .FromSender(this)
                .AddReceivers<PlayConfigPageViewModel>()
                .SetAsOneTime()
                .PublishAsync();
        }
        catch (Exception ex)
        {
            await Log.ErrorAsync($"初始化音乐项出错: {ex.Message}");
        }
    }

    private async Task InitializePlaylistAsync()
    {
        try
        {
            await PlayList.LoadAsync();

            if (PlayList.LatestPlayedMusic == null)
                return;

            var currentMusicItem = PlayList.MusicItems.FirstOrDefault(model =>
                model.FilePath == PlayList.LatestPlayedMusic
            );

            if (currentMusicItem != null)
                await SetCurrentMusicItem(currentMusicItem);
        }
        catch (Exception e)
        {
            await Log.ErrorAsync($"初始化播放列表时出错: {e.Message}");
            throw;
        }
    }

    #endregion

    #region 事件处理

    private static void MusicItemsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        PlayedIndicesService.ClearPlayedIndices();
    }

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
            await Log.ErrorAsync($"音频播放完成后切换下一首音频时遇到错误：{ex.Message}");
        }
    }

    private void ExitReminderMessageHandler(ExitReminderMessage message, object? sender)
    {
        _audioPlay.PositionChanged -= OnPositionChanged;
        _audioPlay.PlaybackCompleted -= AudioPlayOnPlaybackCompleted;
        PlayList.MusicItems.CollectionChanged -= MusicItemsOnCollectionChanged;

        _audioEngine.Dispose();
        _audioPlay.Dispose();
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

    [RelayCommand]
    private void ClearMusicItemCurrentDuration(MusicItemModel musicItem)
    {
        if (musicItem.Equals(CurrentMusicItem))
            CurrentDurationInSeconds = 0;
        else
            musicItem.Current = TimeSpan.Zero;
    }

    #endregion

    #region 播放列表管理

    [RelayCommand]
    private void AddToCurrentPlaylistNextItem(MusicItemModel musicItem)
    {
        RemoveInMusicList(musicItem);
        PlayList.MusicItems.Insert(CurrentIndex + 1, musicItem);
    }

    [RelayCommand]
    private void RemoveInMusicList(MusicItemModel musicItem) => PlayList.MusicItems.Remove(musicItem);

    [RelayCommand]
    private async Task TogglePlaylist(MusicListModel musicList)
    {
        PlayList.MusicItems = new ObservableCollection<MusicItemModel>(musicList.MusicItems);

        if (musicList.MusicItems.Count <= 0)
            return;

        MusicItemModel? selectedMusic = null;

        // 如果有最近播放记录，尝试找到对应歌曲
        if (musicList.LatestPlayedMusic != null)
        {
            selectedMusic = musicList.MusicItems.FirstOrDefault(x => x.FilePath == musicList.LatestPlayedMusic);
        }

        // 如果没有找到最近播放的，就选第一个
        selectedMusic ??= musicList.MusicItems.First();

        await PlaySpecifiedMusic(selectedMusic);
    }

    #endregion

    #region 音乐项管理

    [RelayCommand]
    private static async Task ShowDialog(MusicItemModel musicItem)
    {
        var options = new OverlayDialogOptions
        {
            Title = "详细信息",
            Buttons = DialogButton.None,
            CanLightDismiss = true,
            Mode = DialogMode.Info,
            CanDragMove = true,
            CanResize = false,
        };

        await OverlayDialog.ShowModal<AudioDetailedInfo, AudioDetailedInfoViewModel>(
            new AudioDetailedInfoViewModel(musicItem, await musicItem.GetExtensionsInfo()),
            options: options
        );
    }

    [RelayCommand]
    private static void OpenInExplorer(MusicItemModel musicItem)
    {
        if (string.IsNullOrEmpty(musicItem.FilePath) || !File.Exists(musicItem.FilePath))
        {
            NotificationService.ShowLight(
                new Notification("坏欸", $"无法打开《{musicItem.Title}》文件位置：文件不存在"),
                NotificationType.Error,
                showClose: false
            );
            return;
        }

        PathEnsurer.OpenInExplorer(musicItem.FilePath);
    }

    [RelayCommand]
    private async Task DeleteMusicItem(MusicItemModel musicItem)
    {
        var result = await MessageBox.ShowOverlayAsync(
            $"你真的要删除《{musicItem.Title}》吗?",
            "警告",
            icon: MessageBoxIcon.Warning,
            button: MessageBoxButton.YesNo
        );
        if (result != MessageBoxResult.Yes)
            return;

        bool isSuccess =
            await DataBaseService.DeleteDataAsync(
                DataBaseService.Table.MUSICS,
                nameof(MusicItemModel.FilePath),
                musicItem.FilePath
            ) != DataBaseService.OperationResult.Failure
            || await DataBaseService.DeleteDataAsync(
                DataBaseService.Table.MUSICLISTS,
                nameof(MusicItemModel.FilePath),
                musicItem.FilePath
            ) != DataBaseService.OperationResult.Failure;

        if (isSuccess)
        {
            MusicItems.Remove(musicItem);
            PlayList.MusicItems.Remove(musicItem);

            NotificationService.ShowLight(
                new Notification("好欸", $"《{musicItem.Title}》已经从音乐列表中移除了！"),
                NotificationType.Success
            );
        }
        else
        {
            NotificationService.ShowLight(
                new Notification("坏欸", $"《{musicItem.Title}》删除失败了！"),
                NotificationType.Error
            );
        }
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

        if (PlayList.Count == 0)
            PlayList.MusicItems = new ObservableCollection<MusicItemModel>(MusicItems);

        var item = MusicItems.FirstOrDefault();
        if (item != null)
            await SetCurrentMusicItem(item);
        return item == null;
    }

    private async Task SetAndPlay(int index)
    {
        if (index < 0 || index >= PlayList.Count)
            return;
        await SetCurrentMusicItem(PlayList.MusicItems[index], PlayerConfig.IsRestartPlay);
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
        PlayList.Count switch
        {
            <= 0 => -1,
            1 => 0,
            _ => PlayerConfig is { PlayMode: PlayMode.Random, IsRealRandom: true }
                ? PlayedIndicesService.GetNonRepeatingRandomIndex(current, PlayList.Count)
            : isNext ? (current + 1) % PlayList.Count
            : (current - 1 + PlayList.Count) % PlayList.Count,
        };

    private void ShufflePlaylist()
    {
        if (PlayList.Count <= 1)
            return;

        // 保存当前播放的歌曲
        var currentItem = CurrentMusicItem;

        // 创建临时列表并打乱
        var tempList = PlayList.MusicItems.ToList();
        var random = new Random();

        // Fisher-Yates 洗牌算法
        for (int i = tempList.Count - 1; i > 0; i--)
        {
            int j = random.Next(0, i + 1);
            (tempList[i], tempList[j]) = (tempList[j], tempList[i]);
        }

        // 如果当前有播放的歌曲，确保它在列表的当前位置
        if (PlayList.MusicItems.Contains(currentItem))
        {
            int currentIndex = CurrentIndex;
            tempList.Remove(currentItem);
            tempList.Insert(currentIndex, currentItem);
        }

        // 更新播放列表
        PlayList.MusicItems = new ObservableCollection<MusicItemModel>(tempList);
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


    public async Task SaveAsync()
    {
        try
        {
            foreach (var item in MusicItems)
            {
                if (!item.IsModified || item.IsError)
                    continue;
                await SaveMusicItemAsync(item);
            } // 保存音乐项

            await SaveMusicListAsync(PlayList); // 保存播放列表
        }
        catch (Exception e)
        {
            await Log.ErrorAsync($"数据库保存失败 : {e.Message}");
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
            await Log.ErrorAsync($"保存音乐项失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 保存歌单到数据库
    /// </summary>
    private static async Task SaveMusicListAsync(MusicListModel musicList)
    {
        try
        {
            // 获取当前播放列表中已存在的歌曲路径
            var existingPaths = await DataBaseService.LoadSpecifyFieldsAsync(
                DataBaseService.Table.MUSICLISTS,
                [nameof(MusicItemModel.FilePath)],
                dict => dict.TryGetValue(nameof(MusicItemModel.FilePath), out object? path) ? path.ToString() : null,
                search: $"{nameof(MusicListModel.Name)} = '{musicList.Name.Replace("'", "''")}'"
            );

            var existingPathsSet = new HashSet<string?>(existingPaths);

            // 保存播放列表中的每首歌曲，如果已存在则跳过
            foreach (var musicItem in musicList.MusicItems)
            {
                // 如果歌曲已存在于播放列表中，则跳过
                if (existingPathsSet.Contains(musicItem.FilePath))
                    continue;

                var playlistData = new Dictionary<string, string?>
                {
                    [nameof(MusicListModel.Name)] = musicList.Name,
                    [nameof(MusicItemModel.FilePath)] = musicItem.FilePath,
                };

                await DataBaseService.InsertDataAsync(playlistData, DataBaseService.Table.MUSICLISTS);
            }

            // 删除不再存在于播放列表中的歌曲
            var currentPaths = new HashSet<string?>(musicList.MusicItems.Select(item => item.FilePath));
            foreach (string path in existingPathsSet.OfType<string>().Where(path => !currentPaths.Contains(path)))
            {
                await DataBaseService.DeleteDataAsync(
                    DataBaseService.Table.MUSICLISTS,
                    nameof(MusicItemModel.FilePath),
                    path
                );
            }

            // 更新或插入播放列表名称记录
            var listNameData = musicList.Dump();

            bool exists = await DataBaseService.RecordExistsAsync(
                DataBaseService.Table.LISTINFO,
                nameof(MusicListModel.Name),
                musicList.Name
            );

            if (exists)
            {
                await DataBaseService.UpdateDataAsync(
                    listNameData,
                    DataBaseService.Table.LISTINFO,
                    nameof(MusicListModel.Name),
                    musicList.Name
                );
            }
            else
            {
                await DataBaseService.InsertDataAsync(listNameData, DataBaseService.Table.LISTINFO);
            }
        }
        catch (Exception ex)
        {
            await Log.ErrorAsync($"保存播放列表失败: {ex.Message}");
        }
    }

    #endregion

    #region 音频处理

    public async Task SetCurrentMusicItem(MusicItemModel musicItem, bool restart = false)
    {
        if (!PlayList.MusicItems.Contains(musicItem))
        {
            PlayList = new MusicListModel { MusicItems = new ObservableCollection<MusicItemModel>(MusicItems) };
        }

        if (restart || IsNearEnd(musicItem))
        {
            musicItem.Current = TimeSpan.Zero;
        }

        _audioPlay.Stop();

        try
        {
            await InitializeAudioTrackAsync(musicItem);
            _isSlideCutting = true;
            CurrentMusicItem = musicItem;
            _isSlideCutting = false;

            LyricsModel = new LyricsModel(await musicItem.Lyrics);

            CurrentDurationInSeconds = musicItem.Current.TotalSeconds;
            CurrentMusicItemChanged?.Invoke(this, musicItem);
            PlayList.LatestPlayedMusic = CurrentMusicItem.FilePath;
        }
        catch (Exception ex)
        {
            await Log.ErrorAsync($"初始化音轨失败: {ex.Message}");
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
            await Log.ErrorAsync($"设置采样率时出现错误 : {e.Message}");
        }
    }

    #endregion
}
