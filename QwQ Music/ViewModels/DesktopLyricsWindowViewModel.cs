using CommunityToolkit.Mvvm.ComponentModel;
using QwQ_Music.Models;
using QwQ_Music.Models.ConfigModel;
using QwQ_Music.ViewModels.ViewModelBases;

namespace QwQ_Music.ViewModels;

public partial class DesktopLyricsWindowViewModel : ViewModelBase
{
    public DesktopLyricsWindowViewModel()
    {
        MusicPlayerViewModel.LyricsModel.LyricLineChanged += LyricsModelOnLyricLineChanged;
    }

    public void Unsubscribe()
    {
        MusicPlayerViewModel.LyricsModel.LyricLineChanged -= LyricsModelOnLyricLineChanged;
    }

    public MusicPlayerViewModel MusicPlayerViewModel { get; } = MusicPlayerViewModel.Instance;

    public static DesktopLyricConfig LyricConfig => ConfigInfoModel.LyricConfig.DesktopLyric;

    [ObservableProperty]
    public partial string CurrentMainLyric { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string CurrentAltLyric { get; set; } = string.Empty;

    private void LyricsModelOnLyricLineChanged(object sender, LyricLine currentLyric, LyricLine? nextLyric)
    {
        CurrentMainLyric = currentLyric.Primary;
        if (nextLyric is { } nextLyricLine)
        {
            CurrentAltLyric = nextLyricLine.Primary;
        }
    }
}
