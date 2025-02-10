using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using QwQ_Music.Common;
using QwQ_Music.Models;
using QwQ_Music.Tools;

namespace QwQ_Music.ViewModels;

public sealed partial class MusicPlayerViewModel : ViewModelBase
{
    private static readonly Lazy<MusicPlayerViewModel> _instance = new(() => new MusicPlayerViewModel());

    private AudioFileReader? _audioFileReader;

    [ObservableProperty] private double _currentDurationInSeconds;
    private int _currentIndex;

    [ObservableProperty] private MusicItemModel _currentMusicItem = new("听你想听~", "YOU");
    private bool _isAutoChange;

    [ObservableProperty] private bool _isPlaying;

    [ObservableProperty] private ObservableCollection<MusicItemModel> _musicItems = [];

    [ObservableProperty] private ObservableCollection<MusicItemModel> _musicPlaylist = [];
    private DispatcherTimer? _progressTimer;
    private WaveOutEvent? _waveOutEvent;

    private MusicPlayerViewModel()
    {
        InitializeCurrentMusicItemAsync().ConfigureAwait(false);
    }
    public static MusicPlayerViewModel Instance => _instance.Value;

    public event EventHandler<bool>? PlaybackStateChanged;

    public event EventHandler<(MusicItemModel oldMusic, MusicItemModel newMusic)>? CurrentMusicChanged;

    public void UpdateMusicPlaylist(MusicItemModel currentMusicItem, ObservableCollection<MusicItemModel>? musicList = null)
    {
        if (musicList != null) MusicPlaylist = !MusicPlaylist.SequenceEqual(musicList) ? musicList : MusicItems;

        if (CurrentMusicItem == currentMusicItem) return;

        CurrentMusicItem = currentMusicItem;
    }

    private async Task InitializeCurrentMusicItemAsync()
    {
        try
        {
            var musicItems = await ConfigService.GetMusicInfoAsync();
            if (musicItems != null)
            {
                MusicItems = musicItems;
            }

            var cachePlayList = await ConfigService.GetMusicListAsync();

            if (cachePlayList == null) return;

            string? currentMusicPath = cachePlayList.CurrentMusicPath;
            List<MusicItemModel> musicPlayList = [];
            musicPlayList.AddRange(cachePlayList.MusicPlayList.Select(musicItemPath => MusicItems.FirstOrDefault(p => p.FilePath == musicItemPath)).OfType<MusicItemModel>());
            MusicPlaylist = new ObservableCollection<MusicItemModel>(musicPlayList);
            var currentMusicItem = MusicPlaylist.FirstOrDefault(musicItem => musicItem.FilePath == currentMusicPath);

            if (currentMusicItem == null) return;

            CurrentMusicItem = currentMusicItem;

        }
        catch
        {
            Console.WriteLine("初始化音乐列表发生错误！");
        }
    }

    private void StopAndDisposeCurrentTrack()
    {
        IsPlaying = false;

        _waveOutEvent?.Stop();
        _waveOutEvent?.Dispose();
        _waveOutEvent = null;

        _audioFileReader?.Dispose();
        _audioFileReader = null;

        // 停止并释放定时器
        if (_progressTimer != null)
        {
            _progressTimer.Stop();
            _progressTimer.Tick -= OnProgressTimerTick;
        }
        _progressTimer = null;
    }

    partial void OnCurrentMusicItemChanged(MusicItemModel value)
    {
        StopAndDisposeCurrentTrack();

        if (value.FilePath == null) return;

        _currentIndex = MusicPlaylist.IndexOf(CurrentMusicItem);
        if (_currentIndex == -1)
            _currentIndex = 0; // 默认从第一首开始播放

        CurrentDurationInSeconds = value.CurrentDuration.ParseSeconds();
        InitializeNewTrack();

        CurrentMusicChanged?.Invoke(this, (value, _currentMusicItem));
    }

    private void InitializeNewTrack()
    {
        try
        {
            _audioFileReader = new AudioFileReader(CurrentMusicItem.FilePath!);

            // 设置初始位置（边界检查）
            var targetTime = TimeSpan.FromSeconds(CurrentDurationInSeconds);
            if (targetTime > _audioFileReader.TotalTime)
                targetTime = _audioFileReader.TotalTime;

            var offsetSampleProvider = new OffsetSampleProvider(_audioFileReader)
            {
                SkipOver = targetTime,
            };

            // 初始化播放设备
            _waveOutEvent = new WaveOutEvent();
            _waveOutEvent.PlaybackStopped += OnPlaybackStopped;
            _waveOutEvent.Init(offsetSampleProvider);

            // 初始化定时器
            _progressTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1000),
            };
            _progressTimer.Tick += OnProgressTimerTick;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"初始化音轨失败: {ex.Message}");
        }
    }


    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        if (e.Exception != null)
        {
            // 处理播放异常
            Console.WriteLine($"播放错误: {e.Exception.Message}");
        }

        // 自动播放下一首
        if (_waveOutEvent?.PlaybackState == PlaybackState.Stopped)
        {
            if (_audioFileReader == null) return;

            double currentSeconds = _audioFileReader.CurrentTime.TotalSeconds;
            double targetSeconds = CurrentMusicItem.TotalDuration.ParseSeconds();

            if (!(Math.Abs(currentSeconds - targetSeconds) < 0.1)) return;

            CurrentMusicItem.CurrentDuration = TimeConverter.FormatSeconds(0);
            ToggleNextSong();
            CurrentDurationInSeconds = 0;
            IsPlaying = true;

        }
    }

    private void OnProgressTimerTick(object? sender, EventArgs e)
    {
        if (_audioFileReader == null || _waveOutEvent == null) return;

        _isAutoChange = true;
        CurrentDurationInSeconds = _audioFileReader.CurrentTime.TotalSeconds;
        _isAutoChange = false;
    }

    partial void OnIsPlayingChanged(bool value)
    {
        PlaybackStateChanged?.Invoke(this, value);
        UpdatePlaybackState();
    }

    private void UpdatePlaybackState()
    {
        if (_waveOutEvent == null) return;

        if (IsPlaying)
        {
            // 先启动定时器再开始播放
            _progressTimer?.Start();
            _waveOutEvent.Play();
        }
        else
        {
            _waveOutEvent.Pause();
            _progressTimer?.Stop();
        }
    }

    [RelayCommand]
    private void StartOrStopPlayback()
    {
        if (MusicPlaylist.Count == 0)
            MusicPlaylist = MusicItems;

        if (MusicPlaylist.Count > 0)
            if (CurrentMusicItem.FilePath == null)
            {
                var item = MusicPlaylist.FirstOrDefault(musicItem => musicItem.FilePath != null);
                if (item != null)
                    CurrentMusicItem = item;
                else
                    return;
            }

        IsPlaying = !IsPlaying;
    }

    [RelayCommand]
    private void PlaySpecifiedMusic(MusicItemModel? musicItem)
    {
        if (musicItem == null) return;
        UpdateMusicPlaylist(musicItem);
        StartOrStopPlayback();
    }

    [RelayCommand]
    private void TogglePreviousSong()
    {
        if (MusicPlaylist.Count == 0 || _currentIndex == -1) return;

        _currentIndex--;
        if (_currentIndex < 0) _currentIndex = MusicPlaylist.Count - 1;
        CurrentMusicItem = MusicPlaylist[_currentIndex];
        IsPlaying = true;
    }

    [RelayCommand]
    private void ToggleNextSong()
    {
        if (MusicPlaylist.Count == 0 || _currentIndex == -1) return;

        _currentIndex++;
        if (_currentIndex >= MusicPlaylist.Count) _currentIndex = 0;
        CurrentMusicItem = MusicPlaylist[_currentIndex];
        IsPlaying = true;
    }

    partial void OnCurrentDurationInSecondsChanged(double value)
    {
        CurrentMusicItem.CurrentDuration = value.FormatSeconds();

        if (_isAutoChange || _audioFileReader == null) return;

        if (IsPlaying)
        {
            _audioFileReader.CurrentTime = TimeSpan.FromSeconds(value);
        }
        else
        {
            StopAndDisposeCurrentTrack();
            InitializeNewTrack();
        }
    }


    public void SaveMusicInfoAsync()
    {
        if (MusicItems.Count == 0) return;
        _ = ConfigService.SaveMusicInfoAsync(MusicItems);
    }

    public void SaveMusicListAsync()
    {
        List<string?> filePaths = [];
        if (MusicPlaylist.Count > 0)
        {
            filePaths = MusicPlaylist
                .Select(item => item.FilePath)
                .ToList();
        }

        _ = ConfigService.SaveMusicListAsync(new MusicListModel(CurrentMusicItem.FilePath, filePaths));
    }

    public void CleaningAndRelease()
    {
        IsPlaying = false;
        StopAndDisposeCurrentTrack();
    }
}
