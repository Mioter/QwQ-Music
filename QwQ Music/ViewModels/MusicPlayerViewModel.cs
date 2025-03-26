using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Models;
using QwQ_Music.Models.ConfigModel;
using QwQ_Music.Services;
using QwQ_Music.Services.Audio;
using QwQ_Music.Services.ConfigIO;
using QwQ_Music.Utilities.MessageBus;
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

    private readonly AudioPlay _audioPlay = new();

    [ObservableProperty]
    public partial double CurrentDurationInSeconds { get; set; }

    [ObservableProperty]
    public partial MusicItemModel CurrentMusicItem { get; set; } = new("听你想听~", "YOU");
    [ObservableProperty]
    public partial bool IsPlaying { get; set; }

    [ObservableProperty]
    public partial bool IsMuted { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<MusicItemModel> MusicItems { get; set; } = [];
    public static PlaylistModel Playlist { get; } = new(PlayerConfig.LatestPlayListName);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VolumePercent))]
    private partial float Volume { get; set; } = 1.0f;

    private bool _isAutoChange;
    private int CurrentIndex => Playlist.MusicItems.IndexOf(CurrentMusicItem);
    
    public int VolumePercent
    {
        get => (int)(Volume * 100);
        set => Volume = Math.Clamp(value / 100f, 0f, 1.0f);
    }
    
    #endregion

    #region 事件
    
    public event EventHandler<bool>? PlaybackStateChanged;
    public event EventHandler<MusicItemModel>? CurrentMusicItemChanging;
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
    
    partial void OnVolumeChanged(float value)
    {
        _audioPlay.SetVolume(value);
        IsMuted = Volume == 0f;
    }
    
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
            await ToggleNextSong();
            CurrentMusicItem.Current = TimeSpan.Zero;
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

        _audioPlay.Dispose();
        SaveConfig();
    }
    
    #endregion

    #region 播放控制方法
    
    private void UpdatePlaybackState(bool isPlaying)
    {
        if (isPlaying)
            _audioPlay.Play();
        else
            _audioPlay.Pause();
    }

    [RelayCommand]
    private async Task TogglePlayback()
    {
        if (await FallbackMusicItem())
            return;

        if (Playlist.Count == 0)
            Playlist.MusicItems = new ObservableCollection<MusicItemModel>(MusicItems);

        IsPlaying = !IsPlaying;
        UpdatePlaybackState(IsPlaying);
    }

    [RelayCommand]
    private async Task PlaySpecifiedMusic(MusicItemModel? musicItem)
    {
        if (musicItem == null)
            return;

        if (CurrentMusicItem.Equals(musicItem))
        {
            IsPlaying = !IsPlaying;
        }
        else
        {
            await SetCurrentMusicItem(musicItem, true);
            IsPlaying = true;
        }

        UpdatePlaybackState(IsPlaying);
    }

    [RelayCommand]
    private async Task TogglePreviousSong()
    {
        if (Playlist.Count <= 0)
            return;
        await SetAndPlay(GetPreviousIndex(CurrentIndex));
    }

    [RelayCommand]
    private async Task ToggleNextSong()
    {
        if (Playlist.Count <= 0)
            return;
        await SetAndPlay(GetNextIndex(CurrentIndex));
    }

    [RelayCommand]
    private void ToggleMuteMode()
    {
        IsMuted = !IsMuted;
        if (IsMuted)
        {
            ConfigInfoModel.PlayerConfig.IsMuted = true;
            ConfigInfoModel.PlayerConfig.Volume = VolumePercent;
        }

        /*AudioPlay.IsMute = IsMuted;*/
        VolumePercent = IsMuted ? 0 : ConfigInfoModel.PlayerConfig.Volume;
    }
    
    [RelayCommand]
    private async Task RefreshCurrentMusicItem()
    {
        await SetCurrentMusicItem(CurrentMusicItem, true);
        IsPlaying = true;
        UpdatePlaybackState(IsPlaying);
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
    
    private async Task<bool> FallbackMusicItem()
    {
        if (File.Exists(CurrentMusicItem.FilePath))
            return false;

        var item = MusicItems.FirstOrDefault();
        if (item != null)
            await SetCurrentMusicItem(item);
        return item == null;
    }

    private async Task SetAndPlay(int index)
    {
        if (index < 0 || index >= Playlist.Count)
            return;
        await SetCurrentMusicItem(Playlist.MusicItems[index]);
        IsPlaying = true;
        UpdatePlaybackState(IsPlaying);
    }

    private static int GetNextIndex(int current) => (current + 1) % Playlist.Count;

    private static int GetPreviousIndex(int current) => (current - 1 + Playlist.Count) % Playlist.Count;
    
    private static bool IsNearEnd(MusicItemModel musicItem) =>
        Math.Abs(musicItem.Duration.TotalSeconds - musicItem.Current.TotalSeconds) < 0.5;
    
    #endregion

    #region 数据持久化
    
    private static void SaveMusicInfo(ObservableCollection<MusicItemModel> musicItems)
    {
        foreach (var item in musicItems)
            DataBaseService.SaveToDatabaseAsync(item, DataBaseService.Table.MUSICS).ConfigureAwait(false);
    }

    private void SaveConfig()
    {
        SaveMusicInfo(MusicItems);
    }
    
    #endregion

    #region 音频处理
    
    public async Task SetCurrentMusicItem(MusicItemModel musicItem, bool restart = false)
    {
        if (!Playlist.MusicItems.Contains(musicItem))
        {
            Playlist.MusicItems = new ObservableCollection<MusicItemModel>(MusicItems);
        }

        if (musicItem.Equals(CurrentMusicItem) && !restart)
            return;

        if (restart || IsNearEnd(musicItem))
        {
            musicItem.Current = TimeSpan.Zero;
        }
        
        try
        {
            CurrentMusicItemChanging?.Invoke(this, musicItem);
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
    
    #endregion
}
