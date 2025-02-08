using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;

namespace QwQ_Music.ViewModels;

public partial class MusicPlayerTrayViewModel : ViewModelBase
{

    [ObservableProperty] private double _albumCoverCurrentAngle;

    [ObservableProperty] private double _albumCoverRecordAngle;

    [ObservableProperty] private double _playButtonAngle;

    [ObservableProperty] private Thickness _playButtonPadding;

    public MusicPlayerTrayViewModel()
    {
        MusicPlayerViewModel.PlaybackStateChanged += MusicPlayerViewModelOnPlaybackStateChanged;
    }
    public MusicPlayerViewModel MusicPlayerViewModel { get; } = MusicPlayerViewModel.Instance;

    private void MusicPlayerViewModelOnPlaybackStateChanged(object? sender, bool e)
    {
        if (!e) AlbumCoverRecordAngle = AlbumCoverCurrentAngle;
    }
}
