using QwQ_Music.Common.Manager;
using QwQ_Music.Models.ConfigModels;
using QwQ_Music.ViewModels.Bases;

namespace QwQ_Music.ViewModels;

public class DesktopLyricsWindowViewModel : ViewModelBase
{
    public static MusicPlayerViewModel MusicPlayerViewModel => MusicPlayerViewModel.Default;

    public static DesktopLyricConfig LyricConfig => ConfigManager.LyricConfig.DesktopLyric;
}
