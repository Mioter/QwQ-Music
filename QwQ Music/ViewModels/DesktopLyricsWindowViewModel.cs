using CommunityToolkit.Mvvm.ComponentModel;
using QwQ_Music.Models;
using QwQ_Music.ViewModels.ViewModelBases;
using DesktopLyricConfig = QwQ_Music.Models.ConfigModels.DesktopLyricConfig;

namespace QwQ_Music.ViewModels;

public partial class DesktopLyricsWindowViewModel : ViewModelBase
{
    public DesktopLyricsWindowViewModel()
    {
        var currentLyric = MusicPlayerViewModel.LyricsModel.CurrentLyric;
        var nextLyric = MusicPlayerViewModel.LyricsModel.GetNextLyric(currentLyric.TimePoint);
        LyricsModelOnLyricLineChanged(this, MusicPlayerViewModel.LyricsModel.CurrentLyric, nextLyric);

        MusicPlayerViewModel.LyricsModel.LyricLineChanged += LyricsModelOnLyricLineChanged;
    }

    public void Unsubscribe()
    {
        MusicPlayerViewModel.LyricsModel.LyricLineChanged -= LyricsModelOnLyricLineChanged;
    }

    public MusicPlayerViewModel MusicPlayerViewModel { get; } = MusicPlayerViewModel.Instance;

    public static DesktopLyricConfig LyricConfig => ConfigManager.LyricConfig.DesktopLyric;

    [ObservableProperty]
    public partial string? CurrentMainLyric { get; set; }

    [ObservableProperty]
    public partial string? CurrentMainTranslateLyric { get; set; }

    [ObservableProperty]
    public partial string? CurrentAltLyric { get; set; }

    [ObservableProperty]
    public partial string? CurrentAltTranslateLyric { get; set; }

    private void LyricsModelOnLyricLineChanged(object sender, LyricLine currentLyric, LyricLine? nextLyric)
    {
        CurrentMainLyric = currentLyric.Primary;

        CurrentMainTranslateLyric = currentLyric.Translation;

        if (nextLyric is not { } nextLyricLine)
        {
            CurrentAltLyric = null;
            CurrentAltTranslateLyric = null;
            return;
        }

        CurrentAltLyric = nextLyricLine.Primary;
        CurrentAltTranslateLyric = nextLyricLine.Translation;
    }
}
