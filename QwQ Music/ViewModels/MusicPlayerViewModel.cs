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
using QwQ_Music.Services.Audio.Play;
using QwQ_Music.Services.ConfigIO;
using Log = QwQ_Music.Services.LoggerService;

namespace QwQ_Music.ViewModels;

public partial class MusicPlayerViewModel : ViewModelBase
{
    // ReSharper disable once InconsistentNaming
    private static readonly Lazy<MusicPlayerViewModel> _instance = new(() => new MusicPlayerViewModel());
    public static MusicPlayerViewModel Instance => _instance.Value;

    [ObservableProperty]
    private double _currentDurationInSeconds;

    [ObservableProperty]
    private MusicItemModel _currentMusicItem = new("听你想听~", ["YOU"]);
    private bool _isAutoChange;

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private bool _isMuted;

    [ObservableProperty]
    private ObservableCollection<MusicItemModel> _musicItems = [];

    public static PlaylistModel Playlist { get; } = new(PlayerConfig.LatestPlayListName);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VolumePercent))]
    private float _volume = 1.0f;
    public int VolumePercent
    {
        get => (int)(Volume * 100);
        set => Volume = Math.Clamp(value / 100f, 0f, 1.0f);
    }

    private MusicPlayerViewModel()
    {
        InitializeAsync();

        AudioPlay.PositionChanged += OnPositionChanged;
        AudioPlay.PlaybackCompleted += AudioPlayOnPlaybackCompleted;
        ExitReminderService.ExitReminder += ExitReminderServiceOnExitReminder;
    }

    private void ExitReminderServiceOnExitReminder(object? sender, EventArgs e)
    {
        ExitReminderService.ExitReminder -= ExitReminderServiceOnExitReminder;
        AudioPlay.PositionChanged -= OnPositionChanged;
        AudioPlay.PlaybackCompleted -= AudioPlayOnPlaybackCompleted;

        AudioPlay.Dispose();
        SaveMusicInfo(MusicItems);
        SaveConfig();
    }

    public readonly AudioPlay AudioPlay = new();

    private int CurrentIndex => Playlist.MusicItems.IndexOf(CurrentMusicItem);

    partial void OnVolumeChanged(float value)
    {
        AudioPlay.SetVolume(value);
        IsMuted = Volume == 0f;
    }

    public event EventHandler<bool>? PlaybackStateChanged;
    public event EventHandler<MusicItemModel>? CurrentMusicItemChanging;
    public event EventHandler<MusicItemModel>? CurrentMusicItemChanged;
    public event EventHandler<ObservableCollection<MusicItemModel>>? MusicItemsChanged;

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
        catch
        {
            Log.Error("Unexpected error occured while initializing music playlist.");
        }
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
            await ToggleNextSong();
            CurrentMusicItem.Current = TimeSpan.Zero;
        }
        catch (Exception ex)
        {
            Log.Error($"音频播放完成后切换下一首音频时遇到错误：{ex.Message}");
        }
    }

    private void UpdatePlaybackState(bool isPlaying)
    {
        if (isPlaying)
            AudioPlay.PlayWithFade();
        else
            AudioPlay.StopWithFade();
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

    private async Task<bool> FallbackMusicItem()
    {
        if (File.Exists(CurrentMusicItem.FilePath))
            return false;

        var item = MusicItems.FirstOrDefault();
        if (item != null)
            await SetCurrentMusicItem(item);
        return item == null;
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

    private async Task SetAndPlay(int index)
    {
        if (index < 0 || index >= Playlist.Count)
            return;
        await SetCurrentMusicItem(Playlist.MusicItems[index]);
        IsPlaying = true;
        UpdatePlaybackState(true);
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

        VolumePercent = IsMuted ? 0 : ConfigInfoModel.PlayerConfig.Volume;
    }

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
            AudioPlay.Seek(0);
        musicItem.Duration = TimeSpan.Zero;
    }

    [RelayCommand]
    private async Task RefreshCurrentMusicItem()
    {
        await SetCurrentMusicItem(CurrentMusicItem, true);
        UpdatePlaybackState(IsPlaying);
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

        // 如果播放器已启动，则直接 Seek 到指定位置
        if (IsPlaying)
        {
            AudioPlay.Seek(value);
        }
        else
        {
            // 如果播放器未启动，则记录起始位置
            AudioPlay.SetAudioTrack(CurrentMusicItem.FilePath, value, CurrentMusicItem.Gain);
        }
    }

    private static void SaveMusicInfo(ObservableCollection<MusicItemModel> musicItems)
    {
        foreach (var item in musicItems)
            DataBaseService.SaveToDatabaseAsync(item, DataBaseService.Table.MUSICS).ConfigureAwait(false);
    }

    private void SaveConfig()
    {
        SaveMusicInfo(MusicItems);
    }

    public async Task SetCurrentMusicItem(MusicItemModel musicItem, bool restart = false)
    {
        if (!Playlist.MusicItems.Contains(musicItem))
        {
            Playlist.MusicItems = new ObservableCollection<MusicItemModel>(MusicItems);
        }

        if (musicItem.Equals(CurrentMusicItem) && !restart)
            return;

        IsPlaying = false;

        if (restart || IsNearEnd(musicItem))
        {
            musicItem.Current = TimeSpan.Zero;
        }

        CurrentMusicItemChanging?.Invoke(this, musicItem);
        CurrentMusicItem = musicItem;
        CurrentDurationInSeconds = musicItem.Current.TotalSeconds;
        CurrentMusicItemChanged?.Invoke(this, musicItem);

        try
        {
            await InitializeAudioTrackAsync(musicItem);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to initialize audio track: {ex.Message}");
        }
    }

    private static bool IsNearEnd(MusicItemModel musicItem) =>
        Math.Abs(musicItem.Duration.TotalSeconds - musicItem.Current.TotalSeconds) < 0.5;

    private async Task InitializeAudioTrackAsync(MusicItemModel musicItem)
    {
        await Task.Run(() =>
        {
            if (musicItem.Gain <= 0f)
            {
                musicItem.Gain = ReplayGainCalculator.CalculateGain(musicItem.FilePath);
            }

            AudioPlay.SetAudioTrack(musicItem.FilePath, musicItem.Current.TotalSeconds, musicItem.Gain);
        });
    }

    private static int GetNextIndex(int current) => (current + 1) % Playlist.Count;

    private static int GetPreviousIndex(int current) => (current - 1 + Playlist.Count) % Playlist.Count;
}
