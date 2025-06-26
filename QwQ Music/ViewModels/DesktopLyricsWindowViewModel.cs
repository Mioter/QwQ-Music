using QwQ_Music.Models;
using QwQ_Music.ViewModels.ViewModelBases;
using DesktopLyricConfig = QwQ_Music.Models.ConfigModels.DesktopLyricConfig;

namespace QwQ_Music.ViewModels;

public class DesktopLyricsWindowViewModel : ViewModelBase
{
    public MusicPlayerViewModel MusicPlayerViewModel { get; } = MusicPlayerViewModel.Instance;

    public static DesktopLyricConfig LyricConfig => ConfigManager.LyricConfig.DesktopLyric;
}
