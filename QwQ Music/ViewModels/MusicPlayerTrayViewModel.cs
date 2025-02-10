using System;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
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
        MusicPlayerViewModel.CurrentMusicChanged += MusicPlayerViewModelOnCurrentMusicChanged;
    }

    public MusicPlayerViewModel MusicPlayerViewModel { get; } = MusicPlayerViewModel.Instance;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void MusicPlayerViewModelOnPlaybackStateChanged(object? sender, bool e)
    {
        if (!e) AlbumCoverRecordAngle = AlbumCoverCurrentAngle;
    }

    private void MusicPlayerViewModelOnCurrentMusicChanged(object? sender, (MusicItemModel oldMusic, MusicItemModel newMusic) e)
    {
        AlbumCoverRecordAngle = 0;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            MusicPlayerViewModel.PlaybackStateChanged -= MusicPlayerViewModelOnPlaybackStateChanged;
            MusicPlayerViewModel.CurrentMusicChanged -= MusicPlayerViewModelOnCurrentMusicChanged;
        }
    }
}
