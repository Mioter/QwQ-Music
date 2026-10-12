using QwQ_Music.Common.Managers;
using QwQ_Music.Models.ConfigModels;
using QwQ_Music.ViewModels.Bases;

namespace QwQ_Music.ViewModels.Drawers;

public class MusicPlayerTrayViewModel : ViewModelBase {
    public MusicPlayerTrayViewModel() {
        AudioPlayManager.PlaybackStateChanged += AudioPlayManagerOnPlaybackStateChanged;
    }

    public static DrawerManager DrawerManager => DrawerManager.Instance;

    public RolledLyricConfig RolledLyric { get; } = ConfigManager.LyricConfig.RolledLyric;

    public double AlbumCoverCurrentAngle { get; set; }

    public double AlbumCoverRecordAngle { get; set; }

    public static AudioPlayManager AudioPlayManager => AudioPlayManager.Instance;

    private void AudioPlayManagerOnPlaybackStateChanged(object? sender, bool e) {
        if (e)
            return;

        RecordCurrentAngle();
    }

    private void RecordCurrentAngle() {
        AlbumCoverRecordAngle = AlbumCoverCurrentAngle;
        OnPropertyChanged(nameof(AlbumCoverRecordAngle));
    }

    ~MusicPlayerTrayViewModel() { AudioPlayManager.PlaybackStateChanged -= AudioPlayManagerOnPlaybackStateChanged; }
}