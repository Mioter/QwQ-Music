using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Models;
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

    public readonly PlaylistModel Playlist = new(PlayerConfig.LatestPlayListName);
    public PlaylistModel PlaylistProperty => Playlist;

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

        _audioPlay.PositionChanged += OnPositionChanged;
        _audioPlay.PlaybackCompleted += AudioPlayOnPlaybackCompleted;
    }

    private readonly AudioPlay _audioPlay = new();
    public SoundEffectConfigModel SoundEffectConfigModel { get; private set; } = null!;
    private int CurrentIndex => Playlist.MusicItems.IndexOf(CurrentMusicItem);

    partial void OnVolumeChanged(float value)
    {
        _audioPlay.SetVolume(value);
    }

    public event EventHandler<bool>? PlaybackStateChanged;
    public event EventHandler<MusicItemModel>? CurrentMusicItemChanged;
    public event EventHandler<ObservableCollection<MusicItemModel>>? MusicItemsChanged;

    private void InitializeAsync()
    {
        if (!PlayerConfig.IsInitialized)
            JsonService.LoadFromJsonAsync<PlayerConfig>().ConfigureAwait(false);
        InitializeMusicItemAsync().ConfigureAwait(false);

        LoadSoundEffectConfigAsync().ConfigureAwait(false);
    }

    private async Task InitializeMusicItemAsync()
    {
        try
        {
            var wait = Playlist.LoadAsync();
            await foreach (var item in DataBaseService.LoadFromDatabaseAsync<MusicItemModel>())
            {
                MusicItems.Add(item);
            }

            await wait;
            var currentMusicItem = Playlist.MusicItems.FirstOrDefault(model =>
                model.FilePath == Playlist.LatestPlayedMusic
            );
            if (currentMusicItem != null)
                SetCurrentMusicItem(currentMusicItem);
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

    private void AudioPlayOnPlaybackCompleted(object? sender, EventArgs e)
    {
        CurrentMusicItem.Current = TimeSpan.Zero;
        ToggleNextSong();
        CurrentMusicItem.Current = TimeSpan.Zero;
    }

    private void UpdatePlaybackState(bool isPlaying)
    {
        if (Playlist.Count == 0)
            Playlist.MusicItems = new ObservableCollection<MusicItemModel>(MusicItems);

        if (isPlaying)
            _audioPlay.PlayWithFade();
        else
            _audioPlay.StopWithFade();
    }

    [RelayCommand]
    private void TogglePlayback()
    {
        if (FallbackMusicItem())
            return;

        IsPlaying = !IsPlaying;
        UpdatePlaybackState(IsPlaying);
    }

    private bool FallbackMusicItem()
    {
        if (File.Exists(CurrentMusicItem.FilePath))
            return false;

        var item = Playlist.FirstOrDefault();
        if (item != null)
            SetCurrentMusicItem(item);
        return item == null;
    }

    [RelayCommand]
    private void PlaySpecifiedMusic(MusicItemModel? musicItem)
    {
        if (musicItem == null)
            return;

        if (CurrentMusicItem.Equals(musicItem))
        {
            IsPlaying = !IsPlaying;
        }

        if (!CurrentMusicItem.Equals(musicItem))
        {
            SetCurrentMusicItem(musicItem, true);
            IsPlaying = true;
        }

        UpdatePlaybackState(IsPlaying);
    }

    [RelayCommand]
    private void TogglePreviousSong()
    {
        SetAndPlay(GetPreviousIndex(CurrentIndex));
    }

    [RelayCommand]
    private void ToggleNextSong()
    {
        SetAndPlay(GetNextIndex(CurrentIndex));
    }

    private void SetAndPlay(int index)
    {
        SetCurrentMusicItem(Playlist.MusicItems[index]);
        IsPlaying = true;
        UpdatePlaybackState(true);
    }

    [RelayCommand]
    private void ToggleMuteMode()
    {
        IsMuted = !IsMuted;
        if (IsMuted)
            PlayerConfig.IsMuted = true;
        VolumePercent = IsMuted ? 0 : PlayerConfig.Volume;
    }

    [RelayCommand]
    private void AddToMusicListNextItem(MusicItemModel musicItem)
    {
        RemoveInMusicList(musicItem);
        Playlist.MusicItems.Insert(CurrentIndex + 1, musicItem);
    }

    [RelayCommand]
    private void RemoveInMusicList(MusicItemModel musicItem)
    {
        Playlist.MusicItems.Remove(musicItem);
    }

    [RelayCommand]
    private void ClearMusicItemCurrentDuration(MusicItemModel musicItem)
    {
        if (musicItem.Equals(CurrentMusicItem))
            _audioPlay.Seek(0);
        musicItem.Duration = TimeSpan.Zero;
    }

    [RelayCommand]
    private void RefreshCurrentMusicItem()
    {
        SetCurrentMusicItem(CurrentMusicItem, true);
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
            _audioPlay.Seek(value);
        }
        else
        {
            // 如果播放器未启动，则记录起始位置
            _audioPlay.SetAudioTrack(CurrentMusicItem.FilePath, value, CurrentMusicItem.Gain);
        }
    }

    private async Task LoadSoundEffectConfigAsync()
    {
        SoundEffectConfigModel = await JsonConfigService.LoadAsync<SoundEffectConfigModel>("SoundEffectConfig", SoundEffectConfigModelJsonSerializerContext.Default) ?? new SoundEffectConfigModel();
        SoundEffectConfigModel.SetAudioPlay(_audioPlay);
        SoundEffectConfigModel.UpdateAllEffectsConfig();
    }

    private void SaveSoundEffectConfig()
    {
        JsonConfigService.SaveAsync(SoundEffectConfigModel, "SoundEffectConfig",SoundEffectConfigModelJsonSerializerContext.Default).ConfigureAwait(false);
    }

    private static void SaveMusicInfo(ObservableCollection<MusicItemModel> musicItems)
    {
        foreach (var item in musicItems)
            DataBaseService.SaveToDatabaseAsync(item, DataBaseService.Table.MUSICS).ConfigureAwait(false);
    }

    public void CleanupAndRelease()
    {
        IsPlaying = false;
        _audioPlay.Stop();
        _audioPlay.PositionChanged -= OnPositionChanged;

        SaveMusicInfo(MusicItems);
        SaveSoundEffectConfig();
    }

    public void SetCurrentMusicItem(MusicItemModel musicItem, bool restart = false)
    {
        if (Playlist.MusicItems.IndexOf(musicItem) == -1)
            Playlist.MusicItems = new ObservableCollection<MusicItemModel>(MusicItems);

        if (musicItem.Equals(CurrentMusicItem))
            return;

        IsPlaying = false;
        CurrentMusicItem = musicItem;
        if (restart)
        {
            CurrentDurationInSeconds = 0;
        }
        else
        {
            CurrentDurationInSeconds = CurrentMusicItem.Current.TotalSeconds;
            if (Math.Abs(CurrentMusicItem.Duration.TotalSeconds - CurrentDurationInSeconds) < 0.5)
            {
                CurrentDurationInSeconds = 0; // 如果将播放的音乐已播放至结尾，则使已播放进度归零
            }
        }

        CurrentMusicItemChanged?.Invoke(this, musicItem);
        _audioPlay.SetAudioTrack(musicItem.FilePath, CurrentDurationInSeconds, musicItem.Gain);
    }

    private int GetNextIndex(int current) => (current + 1) % Playlist.Count;

    private int GetPreviousIndex(int current) => (current - 1 + Playlist.Count) % Playlist.Count;
}
