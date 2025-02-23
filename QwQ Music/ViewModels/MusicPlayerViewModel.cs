using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Models;
using Log = QwQ_Music.Services.LoggerService;
using QwQ_Music.Services.Audio;
using QwQ_Music.Utilities;

namespace QwQ_Music.ViewModels;

public partial class MusicPlayerViewModel : ViewModelBase {
    // ReSharper disable once InconsistentNaming
    private static readonly Lazy<MusicPlayerViewModel> _instance = new(() => new MusicPlayerViewModel());
    [ObservableProperty] private double _currentDurationInSeconds;
    [ObservableProperty] private MusicItemModel _currentMusicItem = new("听你想听~", ["YOU"]);
    private bool _isAutoChange;
    [ObservableProperty] private bool _isPlaying;
    [ObservableProperty] private bool _isMuted;
    [ObservableProperty] private ObservableCollection<MusicItemModel> _musicItems = [];
    public PlaylistModel Playlist = new(PlayerConfig.LatestPlayListName);
    public PlaylistModel PlaylistProperty => Playlist;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(VolumePercent))]
    private float _volume = 1.0f;

    private MusicPlayerViewModel() {
        InitializeAsync();
        AudioPlay.PositionChanged += OnPositionChanged;
        AudioPlay.PlaybackCompleted += AudioPlayOnPlaybackCompleted;
    }

    public int VolumePercent {
        get => (int)(Volume * 100);
        set => Volume = Math.Clamp(value / 100f, 0f, 1.0f);
    }

    private AudioPlay AudioPlay { get; } = new();
    private int CurrentIndex => Playlist.MusicItems.IndexOf(CurrentMusicItem);
    public static MusicPlayerViewModel Instance => _instance.Value;

    partial void OnVolumeChanged(float value) { AudioPlay.SetVolume(value); }

    public event EventHandler<bool>? PlaybackStateChanged;
    public event EventHandler<MusicItemModel>? CurrentMusicItemChanged;
    public event EventHandler<ObservableCollection<MusicItemModel>>? MusicItemsChanged;

    private void InitializeAsync() {
        if (!PlayerConfig.IsInitialized) ConfigIO.LoadFromJsonAsync<PlayerConfig>().ConfigureAwait(false);
        InitializeMusicItemAsync().ConfigureAwait(false);
    }


    private async Task InitializeMusicItemAsync() {
        try {
            var wait = Playlist.LoadAsync();
            await foreach (MusicItemModel item in ConfigIO.LoadFromDatabaseAsync<MusicItemModel>()) {
                MusicItems.Add(item);
            }

            await wait;
            var currentMusicItem = Playlist.MusicItems.FirstOrDefault( model => model.FilePath == Playlist.LatestPlayedMusic);
            if (currentMusicItem != null) SetCurrentMusicItem(currentMusicItem);
        } catch { Log.Error("Unexpected error occured while initializing music playlist."); }
    }

    private void OnPositionChanged(object? sender, double positionInSeconds) {
        _isAutoChange = true;
        CurrentDurationInSeconds = positionInSeconds;
        _isAutoChange = false;
    }

    private void AudioPlayOnPlaybackCompleted(object? sender, EventArgs e) {
        CurrentMusicItem.Current = TimeSpan.Zero;
        ToggleNextSong();
        CurrentDurationInSeconds = 0;
    }

    private void UpdatePlaybackState(bool isPlaying) {
        if (isPlaying)
            AudioPlay.PlayWithFade(PlayerConfig.FadeInTime); // 淡入
        else
            AudioPlay.StopWithFade(PlayerConfig.FadeOutTime); // 淡出
    }

    [RelayCommand]
    private void TogglePlayback() {
        if (Playlist.Count == 0) Playlist.MusicItems = MusicItems;

        if (FallbackMusicItem()) return;

        IsPlaying = !IsPlaying;
        UpdatePlaybackState(IsPlaying);
    }

    private bool FallbackMusicItem() {
        if (File.Exists(CurrentMusicItem.FilePath)) return false;

        var item = Playlist.FirstOrDefault(item => item.FilePath != null);
        if (item != null) SetCurrentMusicItem(item);
        return item == null;
    }

    [RelayCommand]
    private void PlaySpecifiedMusic(MusicItemModel? musicItem) {
        if (musicItem == null) return;

        if (CurrentMusicItem.Equals(musicItem)) { IsPlaying = !IsPlaying; }

        if (!CurrentMusicItem.Equals(musicItem)) {
            SetCurrentMusicItem(musicItem, true);
            IsPlaying = true;
        }

        UpdatePlaybackState(IsPlaying);
    }

    [RelayCommand] private void TogglePreviousSong() { SetAndPlay(GetPreviousIndex(CurrentIndex)); }

    [RelayCommand] private void ToggleNextSong() { SetAndPlay(GetNextIndex(CurrentIndex)); }

    private void SetAndPlay(int index) {
        SetCurrentMusicItem(Playlist.MusicItems[index]);
        IsPlaying = true;
        UpdatePlaybackState(true);
    }

    [RelayCommand]
    private void ToggleMuteMode() {
        IsMuted = !IsMuted;
        if (IsMuted) PlayerConfig.IsMuted = true;
        VolumePercent = IsMuted ? 0 : PlayerConfig.Volume;
    }

    [RelayCommand]
    private void AddToMusicListNextItem(MusicItemModel musicItem) {
        RemoveInMusicList(musicItem);
        Playlist.MusicItems.Insert(CurrentIndex + 1, musicItem);
    }

    [RelayCommand] private void RemoveInMusicList(MusicItemModel musicItem) { Playlist.MusicItems.Remove(musicItem); }

    partial void OnIsPlayingChanged(bool value) { PlaybackStateChanged?.Invoke(this, value); }

    partial void OnMusicItemsChanged(ObservableCollection<MusicItemModel> value) {
        MusicItemsChanged?.Invoke(this, value);
    }

    partial void OnCurrentDurationInSecondsChanged(double value) {
        CurrentMusicItem.Current = TimeSpan.FromSeconds(value);
        if (_isAutoChange) return;

        AudioPlay.Seek(value);
    }
    
    public void CleanupAndRelease() {
        IsPlaying = false;
        AudioPlay.Stop();
        AudioPlay.PositionChanged -= OnPositionChanged;
    }


    public void SetCurrentMusicItem(MusicItemModel musicItem, bool restart = false) {
        if (Playlist.MusicItems.IndexOf(musicItem) == -1) { Playlist.MusicItems = MusicItems; }

        if (musicItem.FilePath == null || musicItem.Equals(CurrentMusicItem)) return;

        IsPlaying = false;
        CurrentMusicItem = musicItem;
        if (restart) { CurrentDurationInSeconds = 0; } else {
            CurrentDurationInSeconds = CurrentMusicItem.Current.TotalSeconds;
            if (Math.Abs(CurrentMusicItem.Duration.TotalSeconds - CurrentDurationInSeconds) < 0.1) {
                CurrentDurationInSeconds = 0; // 如果将播放的音乐已播放至结尾，则使已播放进度归零
            }
        }

        CurrentMusicItemChanged?.Invoke(this, musicItem);
        AudioPlay.SetAudioTrack(musicItem.FilePath, CurrentDurationInSeconds, musicItem.Gain);
    }

    private int GetNextIndex(int current) { return (current + 1) % Playlist.Count; }

    private int GetPreviousIndex(int current) { return (current - 1 + Playlist.Count) % Playlist.Count; }
}