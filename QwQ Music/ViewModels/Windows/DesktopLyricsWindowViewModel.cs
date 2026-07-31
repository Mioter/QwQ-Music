using QwQ_Music.Common.Managers;
using QwQ_Music.Models.ConfigModels;
using QwQ_Music.ViewModels.Bases;

namespace QwQ_Music.ViewModels.Windows;

public class DesktopLyricsWindowViewModel : ViewModelBase {
    public static AudioPlayManager AudioPlayManager => AudioPlayManager.Instance;
    public static DesktopLyricConfig LyricConfig => ConfigManager.LyricConfig.DesktopLyric;
}