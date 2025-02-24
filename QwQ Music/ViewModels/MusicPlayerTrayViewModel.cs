using System;
using Avalonia;
using Avalonia.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Models;

namespace QwQ_Music.ViewModels;

public partial class MusicPlayerTrayViewModel : ViewModelBase, IDisposable
{

    [ObservableProperty] private double _albumCoverCurrentAngle;

    [ObservableProperty] private double _albumCoverRecordAngle;

    [ObservableProperty] private double _playButtonAngle;

    [ObservableProperty] private Thickness _playButtonPadding;

    public MusicPlayerTrayViewModel()
    {
        MusicPlayerViewModel.PlaybackStateChanged += MusicPlayerViewModelOnPlaybackStateChanged;
        MusicPlayerViewModel.CurrentMusicItemChanged += AudioPlayOnTrackIndexChanged;
    }

    public MusicPlayerViewModel MusicPlayerViewModel { get; } = MusicPlayerViewModel.Instance;
    

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }


    [RelayCommand]
    private void OnVolumeBarPointerWheelChanged(PointerWheelEventArgs? e)
    {
        if (e == null) return;
        // 阻止事件冒泡到父级元素
        e.Handled = true;

        switch (e.Delta.Y)
        {
            // 根据你的需求处理滚轮滚动事件
            case > 0:
                MusicPlayerViewModel.VolumePercent += 2;
                break;
            case < 0:
                MusicPlayerViewModel.VolumePercent -= 2;
                break;
        }
    }


    private void MusicPlayerViewModelOnPlaybackStateChanged(object? sender, bool e)
    {
        if (!e) AlbumCoverRecordAngle = AlbumCoverCurrentAngle;
    }

    private void AudioPlayOnTrackIndexChanged(object? sender, MusicItemModel musicItemModel)
    {
        AlbumCoverRecordAngle = 0;
    }

    public void Dispose(bool disposing)
    {
        if (!disposing) return;

        MusicPlayerViewModel.PlaybackStateChanged -= MusicPlayerViewModelOnPlaybackStateChanged;
        MusicPlayerViewModel.CurrentMusicItemChanged -= AudioPlayOnTrackIndexChanged;
    }
}
