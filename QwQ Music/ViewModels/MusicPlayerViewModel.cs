using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Models;
using QwQ_Music.Services;
using QwQ_Music.Services.Audio;
using QwQ_Music.Utilities;

namespace QwQ_Music.ViewModels;

public partial class MusicPlayerViewModel : ViewModelBase
{
    private static readonly Lazy<MusicPlayerViewModel> _instance = new(() => new MusicPlayerViewModel());
    private readonly ConfigService _configService = new();


    [ObservableProperty] private double _currentDurationInSeconds;
    [ObservableProperty] private MusicItemModel _currentMusicItem = new("听你想听~", "YOU");
    private bool _isAutoChange;
    [ObservableProperty] private bool _isPlaying;
    [ObservableProperty] private bool _isSilent;
    [ObservableProperty] private ObservableCollection<MusicItemModel> _musicItems = [];
    [ObservableProperty] private ObservableCollection<MusicItemModel> _musicPlaylist = [];

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(VolumePercent))]
    private float _volume = 1.0f;

    private MusicPlayerViewModel()
    {
        InitializeAsync();
        AudioPlay.PositionChanged += OnPositionChanged;
        AudioPlay.PlaybackCompleted += AudioPlayOnPlaybackCompleted;
    }

    public int VolumePercent
    {
        get => (int)(Volume * 100);
        set
        {
            IsSilent = value <= 0;
            Volume = Math.Clamp(value / 100f, 0f, 1.0f);
        }
    }

    private AudioPlay AudioPlay { get; } = new();
    private int CurrentIndex => MusicPlaylist.IndexOf(CurrentMusicItem);
    public static MusicPlayerViewModel Instance => _instance.Value;

    partial void OnVolumeChanged(float value)
    {
        AudioPlay.SetVolume(value);
    }

    public event EventHandler<bool>? PlaybackStateChanged;
    public event EventHandler<MusicItemModel>? CurrentMusicItemChanged;
    public event EventHandler<ObservableCollection<MusicItemModel>>? MusicItemsChanged;

    private void InitializeAsync()
    {
        InitializeConfigInfoAsync().ConfigureAwait(false);
        InitializeMusicItemAsync().ConfigureAwait(false);
    }

    private async Task InitializeConfigInfoAsync()
    {
        VolumePercent = await LoadVolumeConfigAsync();
    }

    private async Task InitializeMusicItemAsync()
    {
        try
        {
            var musicItems = await _configService.GetMusicInfoAsync();
            if (musicItems != null)
                MusicItems = musicItems;

            var cachePlayList = await _configService.GetMusicListAsync();
            if (cachePlayList == null) return;

            MusicPlaylist = new ObservableCollection<MusicItemModel>(
                cachePlayList.MusicPlayList
                    .Select(path => MusicItems.FirstOrDefault(item => item.FilePath == path))
                    .Where(item => item != null)!);

            var currentMusicItem = MusicPlaylist.FirstOrDefault(item => item.FilePath == cachePlayList.CurrentMusicPath);
            if (currentMusicItem != null)
                SetCurrentMusicItem(currentMusicItem);
        }
        catch
        {
            Console.WriteLine("初始化音乐列表发生错误！");
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
        CurrentMusicItem.CurrentDuration = "00:00";
        ToggleNextSong();
        CurrentDurationInSeconds = 0;
    }

    private void UpdatePlaybackState(bool isPlaying)
    {
        if (isPlaying)
            AudioPlay.PlayWithFade(1000); // 1秒淡入
        else
            AudioPlay.StopWithFade(1000); // 1秒淡出
    }

    [RelayCommand]
    private void TogglePlayback()
    {
        if (MusicPlaylist.Count == 0)
            MusicPlaylist = MusicItems;

        if (FallbackMusicItem()) return;

        IsPlaying = !IsPlaying;
        UpdatePlaybackState(IsPlaying);
    }

    private bool FallbackMusicItem()
    {
        if (CurrentMusicItem.FilePath != null) return false;

        var item = MusicPlaylist.FirstOrDefault(item => item.FilePath != null);
        if (item != null)
            SetCurrentMusicItem(item);
        return item == null;
    }

    [RelayCommand]
    private void PlaySpecifiedMusic(MusicItemModel? musicItem)
    {
        if (musicItem == null) return;

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
        SetCurrentMusicItem(MusicPlaylist[index]);
        IsPlaying = true;
        UpdatePlaybackState(true);
    }

    [RelayCommand]
    private async Task ToggleSilentModeAsync()
    {
        IsSilent = !IsSilent;
        if (IsSilent) SaveConfigInfoAsync();
        VolumePercent = IsSilent ? 0 : await LoadVolumeConfigAsync();

    }
    
    [RelayCommand]
    private void AddToMusicListNextItem(MusicItemModel musicItem)
    {
        RemoveInMusicList(musicItem);
        MusicPlaylist.Insert(CurrentIndex + 1, musicItem);
    }

    [RelayCommand]
    private void RemoveInMusicList(MusicItemModel musicItem)
    {
        MusicPlaylist.Remove(musicItem);
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
        CurrentMusicItem.CurrentDuration = value.FormatSeconds();
        if (_isAutoChange) return;

        AudioPlay.Seek(value);
    }

    private async Task<int> LoadVolumeConfigAsync()
    {
        var info = await _configService.GetConfigInfoAsync().ConfigureAwait(false); // 避免回到 UI 线程
        return info?.PlayerConfig?.Volume ?? 100;
    }

    public void SaveMusicInfoAsync()
    {
        _ = _configService.SaveMusicInfoAsync(MusicItems);
    }

    public void SaveMusicListAsync()
    {
        var filePaths = MusicPlaylist.Select(item => item.FilePath).ToList();
        _ = _configService.SaveMusicListAsync(new MusicListModel(CurrentMusicItem.FilePath, filePaths));
    }

    public void SaveConfigInfoAsync()
    {
        _ = _configService.SaveConfigInfoAsync(new ConfigInfoModel
        {
            PlayerConfig = new PlayerConfig
            {
                Volume = VolumePercent,
            },
        });
    }

    public void CleanupAndRelease()
    {
        IsPlaying = false;
        AudioPlay.Stop();
        AudioPlay.PositionChanged -= OnPositionChanged;
    }


    public void SetCurrentMusicItem(MusicItemModel musicItem, bool restart = false)
    {
        if (MusicPlaylist.IndexOf(musicItem) == -1)
        {
            MusicPlaylist = MusicItems;
        }

        if (musicItem.FilePath == null || musicItem.Equals(CurrentMusicItem)) return;

        IsPlaying = false;

        musicItem.ReplayGain ??= MultiChannelReplayGain.CalculateMultiChannelReplayGain(musicItem.FilePath);

        CurrentMusicItem = musicItem;
        if (restart)
        {
            CurrentDurationInSeconds = 0;
        }
        else
        {
            CurrentDurationInSeconds = CurrentMusicItem.CurrentDuration.ParseSeconds();
            if (Math.Abs(CurrentMusicItem.TotalDuration.ParseSeconds() - CurrentDurationInSeconds) < 0.1)
            {
                CurrentDurationInSeconds = 0; // 如果将播放的音乐已播放至结尾，则使已播放进度归零
            }
        }

        CurrentMusicItemChanged?.Invoke(this, musicItem);
        AudioPlay.SetAudioTrack(musicItem.FilePath, CurrentDurationInSeconds, musicItem.ReplayGain);
    }

    private int GetNextIndex(int current)
    {
        return (current + 1) % MusicPlaylist.Count;
    }

    private int GetPreviousIndex(int current)
    {
        return (current - 1 + MusicPlaylist.Count) % MusicPlaylist.Count;
    }
}
