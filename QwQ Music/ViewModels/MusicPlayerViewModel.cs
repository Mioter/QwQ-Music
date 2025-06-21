using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Definitions;
using QwQ_Music.Definitions.Enums;
using QwQ_Music.Models;
using QwQ_Music.Services;
using QwQ_Music.Services.Audio;
using QwQ_Music.Services.ConfigIO;
using QwQ_Music.Utilities;
using QwQ_Music.ViewModels.Pages;
using QwQ_Music.ViewModels.UserControls;
using QwQ_Music.ViewModels.ViewModelBases;
using QwQ_Music.Views.UserControls;
using QwQ.Avalonia.Utilities.MessageBus;
using Ursa.Controls;
using Log = QwQ_Music.Services.LoggerService;
using Notification = Ursa.Controls.Notification;
using PlayerConfig = QwQ_Music.Models.ConfigModels.PlayerConfig;

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

        // 初始化歌词滚动定时器
        _lyricsTimer = new Timer();
        _lyricsTimer.Elapsed += OnLyricsTimerElapsed;
        _lyricsTimer.AutoReset = false;

        MusicListsPageViewModel.MusicPlayerViewModel = this;
        // 注册热键功能
        RegisterHotkeyFunctions();
    }

    #endregion

    #region 属性和字段

    private readonly AudioPlay _audioPlay = new();
    private readonly Timer _lyricsTimer;
    private bool _isLyricsTimerEnabled;

    [ObservableProperty]
    public partial MusicItemModel CurrentMusicItem { get; set; } = new() { Title = "听你想听", Artists = "YOU" };

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

    public static PlayerConfig PlayerConfig { get; } = ConfigManager.PlayerConfig;

    public MusicListsPageViewModel MusicListsViewModel { get; } = new();

    public LyricsModel LyricsModel { get; } = new();

    public MusicListModel PlayList { get; } = new(string.Empty);

    public double CurrentPlayPosition
    {
        get => CurrentMusicItem.Current.TotalSeconds;
        set => Seek(value);
    }

    private int CurrentIndex => PlayList.MusicItems.IndexOf(CurrentMusicItem);

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
        try
        {
            await InitializeMusicItemAsync().ConfigureAwait(false); // 加载播放列表

            if (MusicItems.Count == 0)
            {
                NotificationService.ShowLight(
                    new Notification(
                        "呜呜",
                        "真的...一首歌都没有了（ \n Tips : 可以点击右上角加号从文件中添加音乐哦！"
                    ),
                    NotificationType.Information,
                    showClose: true
                );
                return;
            }

            // 合并消息发送和播放列表初始化
            var messageTask = MessageBus
                .CreateMessage(new LoadCompletedMessage(nameof(MusicItems)))
                .FromSender(this)
                .AddReceivers<PlayConfigPageViewModel>()
                .SetAsOneTime()
                .PublishAsync();

            var playlistTask = InitializePlaylistAsync();

            await Task.WhenAll(messageTask, playlistTask, MusicListsViewModel.InitializeAsync(MusicItems))
                .ConfigureAwait(false);
        }
        catch (Exception e)
        {
            await Log.ErrorAsync($"初始化播放器模型出错！\n{e.Message}").ConfigureAwait(false);
        }
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
        }
        catch (Exception ex)
        {
            await Log.ErrorAsync($"初始化音乐项出错: {ex.Message}");
        }
    }

    private async Task InitializePlaylistAsync()
    {
        await PlayList.LoadAsync(MusicItems);

        if (PlayList.LatestPlayedMusic == null)
            return;

        var currentMusicItem = PlayList.MusicItems.FirstOrDefault(model =>
            model.FilePath == PlayList.LatestPlayedMusic
        );

        if (currentMusicItem != null)
        {
            await ToggleMusicAsync(currentMusicItem);
            return;
        }

        if (PlayList.MusicItems.LastOrDefault() is { } musicItem)
        {
            await ToggleMusicAsync(musicItem);
            NotificationService.ShowLight(
                new Notification("不嘻嘻", "上次播放的音乐不在播放列表中，已经切换为当前播放列表第一首~"),
                NotificationType.Information
            );
            return;
        }

        NotificationService.ShowLight(
            new Notification("温馨提示", "当前播放列表没有任何音乐，可以播放任意一首歌，将自动填入播放列表~"),
            NotificationType.Information
        );
    }

    public async Task ShutdownAsync()
    {
        _audioPlay.PositionChanged -= OnPositionChanged;
        _audioPlay.PlaybackCompleted -= AudioPlayOnPlaybackCompleted;
        PlayList.MusicItems.CollectionChanged -= MusicItemsOnCollectionChanged;
        _lyricsTimer.Elapsed -= OnLyricsTimerElapsed;
        _lyricsTimer.Dispose();

        _audioPlay.Dispose();

        await SaveAsync();
    }

    #endregion

    #region 事件处理

    private static void MusicItemsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        PlayedIndicesService.ClearPlayedIndices();
    }

    private void OnPositionChanged(object? sender, double positionInSeconds)
    {
        CurrentMusicItem.Current = TimeSpan.FromSeconds(positionInSeconds);
        OnPropertyChanged(nameof(CurrentPlayPosition));
    }

    private void UpdateLyricsTimer()
    {
        if (!IsPlaying)
            return;

        // 停止当前定时器
        _lyricsTimer.Stop();

        // 计算到下一句歌词的时间间隔
        double nextInterval = LyricsModel.GetNextLyricsInterval(Position);

        if (!(nextInterval > 0))
            return;

        // 设置定时器间隔为到下一句歌词的时间
        _lyricsTimer.Interval = nextInterval * 1000; // 转换为毫秒
        _lyricsTimer.Start();
    }

    private void OnLyricsTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if (!_isLyricsTimerEnabled || !IsPlaying)
            return;

        // 更新当前歌词
        LyricsModel.UpdateLyricsIndex(Position);

        // 计算到下一句歌词的时间间隔
        double nextInterval = LyricsModel.GetNextLyricsInterval(Position);

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
                await RefreshCurrentMusicItem(); // 单曲循环模式下，重新播放当前歌曲
            }
            else if (PlayerConfig.AutoSwitchNext)
            {
                await ToggleNextSong();
            }
            else
            {
                OnPlayingChanged(false);
            }
        }
        catch (Exception ex)
        {
            await Log.ErrorAsync($"音频播放完成后切换下一首音频时遇到错误：{ex.Message}");
        }
    }

    #endregion

    #region 播放控制方法

    [RelayCommand]
    private async Task TogglePlayback()
    {
        if (MusicItems.Count == 0)
        {
            NotificationService.ShowLight(
                new Notification("提示", "音乐库啥也没有，需要我为你播放一首空空如也吗？"),
                NotificationType.Information
            );
            return;
        }

        if (!CurrentMusicItem.IsInitialized)
        {
            var musicItem = MusicItems.FirstOrDefault();
            if (musicItem == null)
            {
                NotificationService.ShowLight(
                    new Notification("错误", "没有找到可播放的音乐项！"),
                    NotificationType.Error
                );
                return;
            }

            await PlaySpecifiedMusic(musicItem);
            return;
        }

        if (await VerifyMusicItem(CurrentMusicItem))
        {
            OnPlayingChanged(!IsPlaying);
        }
    }

    [RelayCommand]
    private async Task PlaySpecifiedMusic(MusicItemModel musicItem)
    {
        if (!await VerifyMusicItem(musicItem))
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
        OnPropertyChanged(nameof(CurrentPlayPosition));

        // 当播放位置改变时，重新设置歌词定时器
        if (IsPlaying)
        {
            UpdateLyricsTimer();
        }
    }

    public async Task ToggleMusicAsync(MusicItemModel musicItem)
    {
        if (await VerifyMusicItem(musicItem))
        {
            OnPlayingChanged(false);
            await SetCurrentMusicItem(musicItem).ConfigureAwait(false);
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
        if (await VerifyMusicItem(CurrentMusicItem))
        {
            await SetCurrentMusicItem(CurrentMusicItem, true);
            OnPlayingChanged(true);
        }
    }

    [RelayCommand]
    private void SetZeroToCurrentPointer(MusicItemModel musicItem)
    {
        if (musicItem.Equals(CurrentMusicItem))
            Seek(0);
        else
            musicItem.Current = TimeSpan.Zero;
    }

    #endregion

    #region 播放列表管理

    [RelayCommand]
    private void AddToCurrentPlaylistNextItem(IList items)
    {
        if (items.Count <= 0)
            return;

        var musicItems = items.Cast<MusicItemModel>().ToList();
        RemoveInMusicList(musicItems);

        // 从后往前插入，这样可以保持原有顺序
        for (int i = musicItems.Count - 1; i >= 0; i--)
        {
            PlayList.MusicItems.Insert(CurrentIndex + 1, musicItems[i]);
        }
    }

    [RelayCommand]
    private void RemoveInMusicList(IList items)
    {
        var musicItems = items.Cast<MusicItemModel>().ToList();
        foreach (var item in musicItems)
        {
            PlayList.MusicItems.Remove(item);
        }
    }

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
            new AudioDetailedInfoViewModel(musicItem, MusicExtractor.ExtractExtensionsInfo(musicItem.FilePath)),
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
                NotificationType.Error
            );
            return;
        }

        try
        {
            PathEnsurer.OpenInFileManager(musicItem.FilePath);
        }
        catch (Exception e)
        {
            Log.Error($"打开文件位置失败: {e.Message}");
            NotificationService.ShowLight(
                new Notification("坏欸", $"打开《{musicItem.Title}》文件位置时报错：{e.Message}"),
                NotificationType.Error
            );
        }
    }

    /// <summary>
    /// 从数据库中批量删除音乐项
    /// </summary>
    /// <param name="items">要删除的音乐项集合</param>
    /// <returns>删除成功的音乐项列表</returns>
    private static async Task<List<MusicItemModel>> DeleteMusicItemsFromDataBaseAsync(IEnumerable<MusicItemModel> items)
    {
        var successItems = new List<MusicItemModel>();
        var itemsList = items.ToList();

        var deleteTasks = itemsList.Select(async item =>
        {
            bool isSuccess =
                await DataBaseService.DeleteDataAsync(
                    DataBaseService.Table.MUSICS,
                    nameof(MusicItemModel.FilePath),
                    item.FilePath
                ) != DataBaseService.OperationResult.Failure
                || await DataBaseService.DeleteDataAsync(
                    DataBaseService.Table.MUSICLISTS,
                    nameof(MusicItemModel.FilePath),
                    item.FilePath
                ) != DataBaseService.OperationResult.Failure;

            if (isSuccess)
            {
                successItems.Add(item);
            }
        });

        // 在后台线程中并行处理所有删除操作
        await Task.WhenAll(deleteTasks);

        return successItems;
    }
    
    public async Task<bool> DeleteMusicItemsAsync(List<MusicItemModel> musicItems)
    {
        if (musicItems.Count == 0)
            return false;

        // 构建确认提示信息
        string titles = string.Join("、", musicItems.Select(item => $"《{item.Title}》"));
        var result = await MessageBox.ShowOverlayAsync(
            $"你真的要删除以下音乐吗？\n{titles}",
            "警告",
            icon: MessageBoxIcon.Warning,
            button: MessageBoxButton.YesNo
        );

        if (result != MessageBoxResult.Yes)
            return false;

        // 批量删除音乐项
        var successItems = await DeleteMusicItemsFromDataBaseAsync(musicItems);
        var successSet = new HashSet<MusicItemModel>(successItems);
        var failedItems = musicItems.Where(item => !successSet.Contains(item)).ToList();

        // 从UI集合中移除已删除的音乐项
        foreach (var item in successItems)
        {
            MusicItems.Remove(item);
            PlayList.MusicItems.Remove(item);

            // 从所有已加载的歌单中移除该音乐
            foreach (var playlist in MusicListsViewModel.PlayListItems)
            {
                if (playlist.IsInitialized)
                {
                    playlist.MusicItems.Remove(item);
                }
            }
        }

        // 显示删除结果通知
        if (successItems.Count > 0)
        {
            string successTitles = string.Join("、", successItems.Select(item => $"《{item.Title}》"));
            NotificationService.ShowLight(
                new Notification("好欸", $"{successTitles}已经从音乐列表中移除了！"),
                NotificationType.Success
            );
        }

        if (failedItems.Count > 0)
        {
            string failedTitles = string.Join("、", failedItems.Select(item => $"《{item.Title}》"));
            NotificationService.ShowLight(
                new Notification("坏欸", $"删除{failedTitles}失败了！"),
                NotificationType.Error
            );
        }
        
        return true;
    }

    #endregion

    #region 辅助方法

    private void OnPlayingChanged(bool value)
    {
        IsPlaying = value;

        if (value)
        {
            _audioPlay.Play();
            _isLyricsTimerEnabled = true;
            UpdateLyricsTimer();
        }
        else
        {
            _audioPlay.Pause();
            _isLyricsTimerEnabled = false;
            _lyricsTimer.Stop();
        }
    }

    private async Task<bool> VerifyMusicItem(MusicItemModel musicItem)
    {
        if (!File.Exists(musicItem.FilePath))
        {
            NotificationService.ShowLight(
                new Notification("错误", $"当前音乐不存在，请切换音乐！\n无法找到音乐文件:  {musicItem.FilePath}"),
                NotificationType.Error
            );
            return false;
        }

        if (!MusicItems.Contains(musicItem))
        {
            await MusicDataPersistence.SaveMusicItemsAsync([musicItem]).ConfigureAwait(false);
            NotificationService.ShowLight(
                new Notification(
                    "注意",
                    $"很奇怪，这个《{musicItem.Title}》不在音乐库中，不过没关系，现在在了，欸嘿~QvQ"
                ),
                NotificationType.Warning
            );
        }

        if (PlayList.Count == 0)
        {
            PlayList.MusicItems = new ObservableCollection<MusicItemModel>(MusicItems);
            NotificationService.ShowLight(
                new Notification("提示", $"当前音乐列表为空，以自动填充为全部音乐！共 {MusicItems.Count} 首~"),
                NotificationType.Information
            );
        }

        if (PlayList.MusicItems.Contains(musicItem))
            return true;

        PlayList.MusicItems.Add(musicItem);

        NotificationService.ShowLight(
            new Notification("提示", $"当前音乐《{musicItem.Title}》不在播放列表中，以自动添加到播放列表末尾~"),
            NotificationType.Information
        );

        return true;
    }

    private async Task SetAndPlay(int index)
    {
        if (index < 0 || index >= PlayList.Count)
            return;

        var musicItem = PlayList.MusicItems[index];

        if (await VerifyMusicItem(musicItem))
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
        Math.Abs(musicItem.Duration.TotalSeconds - musicItem.Current.TotalSeconds) < 5;

    #endregion

    #region 音频处理

    private async Task SetCurrentMusicItem(MusicItemModel musicItem, bool restart = false)
    {
        // 保存上一首歌曲的修改
        if (CurrentMusicItem is { IsInitialized: true, IsModified: true, IsError: false })
        {
            await MusicDataPersistence.SaveMusicItemsAsync([CurrentMusicItem], false).ConfigureAwait(false);
        }

        if (restart || IsNearEnd(musicItem))
        {
            musicItem.Current = TimeSpan.Zero;
        }

        _audioPlay.Stop();

        try
        {
            var lyrics = await musicItem.Lyrics;
            
            if (AudioFileValidator.SupportedAudioFormatsExtend.Contains(Path.GetExtension(musicItem.FilePath).ToUpper(), StringComparer.OrdinalIgnoreCase))
            {
               await InitializeNcmAudioTrackAsync(musicItem).ConfigureAwait(false);
            }
            else
            {
               await Task.Run(() => InitializeAudioTrackAsync(musicItem)).ConfigureAwait(false); 
            }
            
            CurrentPlayPosition = 0;
            CurrentMusicItem = musicItem;
            LyricOffset = musicItem.LyricOffset;
            LyricsModel.UpdateLyricsData(lyrics);
            Seek(musicItem.Current.TotalSeconds);
            
            CurrentMusicItemChanged?.Invoke(this, musicItem);
            PlayList.LatestPlayedMusic = CurrentMusicItem.FilePath;
        }
        catch (Exception ex)
        {
            await Log.ErrorAsync($"初始化新音轨失败: {ex.Message}").ConfigureAwait(false);
        }
    }

    private void InitializeAudioTrackAsync(MusicItemModel musicItem)
    {
        
        // 如果采样率匹配且增益值已设置，直接初始化音频并返回
        if (
            musicItem.Gain > 0f
            && !PlayerConfig.IsAutoSetSampleRate
            && AudioEngineManager.AudioEngine != null
            && PlayerConfig.SampleRate == AudioEngineManager.AudioEngine.SampleRate
        )
        {
            _audioPlay.InitializeAudio(musicItem.FilePath, musicItem.Gain);
            return;
        }

        // 获取音频扩展信息
        var ex = MusicExtractor.ExtractExtensionsInfo(musicItem.FilePath);

        // 处理采样率
        int targetSampleRate = PlayerConfig.IsAutoSetSampleRate ? ex.SamplingRate : PlayerConfig.SampleRate;

        if (AudioEngineManager.AudioEngine != null && targetSampleRate != AudioEngineManager.AudioEngine.SampleRate)
        {
            AudioEngineManager.Create(targetSampleRate);
        }

        // 处理增益值
        if (musicItem.Gain <= 0f)
        {
            musicItem.Gain = AudioHelper.CalcGainOfMusicItem(musicItem.FilePath, ex.SamplingRate, ex.Channels);
        }

        // 初始化音频
        _audioPlay.InitializeAudio(musicItem.FilePath, musicItem.Gain);
    }
    
    private async Task InitializeNcmAudioTrackAsync(MusicItemModel musicItem)
    {
        try
        {
            using var crypt = new NcmdumpCSharp.Core.NeteaseCrypt(musicItem.FilePath);
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

            NotificationService.ShowLight(
                new Notification(
                    "播放失败",
                    $"初始化NCM音轨失败: {musicItem.FilePath}"
                ),
                NotificationType.Error
            );
        }
    }

    #endregion
    
    private async Task SaveAsync()
    {
        try
        {
            if (CurrentMusicItem is { IsInitialized: true, IsModified: true, IsError: false })
            {
                await MusicDataPersistence.SaveMusicItemsAsync([CurrentMusicItem]).ConfigureAwait(false);
            }

            await MusicDataPersistence.SaveMusicListAsync(PlayList).ConfigureAwait(false); // 保存播放列表
        }
        catch (Exception e)
        {
            await Log.ErrorAsync($"数据库保存失败 : {e.Message}");
        }
    }

    /// <summary>
    /// 注册热键功能
    /// </summary>
    private void RegisterHotkeyFunctions()
    {
        HotkeyService.RegisterFunctionAction(
            HotkeyFunction.PreviousSong,
            () => TogglePreviousSongCommand.Execute(null)
        );

        HotkeyService.RegisterFunctionAction(HotkeyFunction.NextSong, () => ToggleNextSongCommand.Execute(null));

        HotkeyService.RegisterFunctionAction(HotkeyFunction.PlayPause, () => TogglePlaybackCommand.Execute(null));

        HotkeyService.RegisterFunctionAction(HotkeyFunction.ToggleMute, () => ToggleMuteModeCommand.Execute(null));

        HotkeyService.RegisterFunctionAction(HotkeyFunction.TogglePlayMode, () => TogglePlayModeCommand.Execute(null));

        HotkeyService.RegisterFunctionAction(
            HotkeyFunction.VolumeUp,
            () =>
            {
                if (Volume < 100)
                    Volume += 5;
            }
        );

        HotkeyService.RegisterFunctionAction(
            HotkeyFunction.VolumeDown,
            () =>
            {
                if (Volume > 0)
                    Volume -= 5;
            }
        );

        HotkeyService.RegisterFunctionAction(
            HotkeyFunction.RefreshCurrentMusic,
            () => RefreshCurrentMusicItemCommand.Execute(null)
        );

        HotkeyService.RegisterFunctionAction(
            HotkeyFunction.ShowPlaylistInfo,
            () =>
            {
                NotificationService.ShowLight(
                    new Notification(
                        "你知道吗？",
                        $"当前播放列表有: {PlayList.MusicItems.Count} 首音乐！\n现在正在播放第 {PlayList.MusicItems.IndexOf(CurrentMusicItem) + 1} 首"
                    ),
                    NotificationType.Information
                );
            }
        );

        HotkeyService.RegisterFunctionAction(
            HotkeyFunction.ShowCurrentInfo,
            () =>
            {
                NotificationService.ShowLight(
                    new Notification(
                        "你知道吗？",
                        $"{(IsPlaying ? "正在播放" : "已暂停")}的音乐叫做: {CurrentMusicItem.Title} 哦！\n你的音量是: {Volume}% "
                    ),
                    NotificationType.Information
                );
            }
        );
    }
}
