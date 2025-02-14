using System.Collections.Generic;
using Avalonia.Layout;
using Avalonia.Media;
using static QwQ_Music.Models.LanguageModel;
using Config = QwQ_Music.Models.DesktopLyricConfig;

namespace QwQ_Music.ViewModels;

public static class DesktopLyricsWindowViewModel {
    public static Dictionary<int, string[]>? Lyrics { get; set; }

    public static int LyricMainFontSize => Config.MainFontSize;
    public static int LyricAltFontSize => Config.AltFontSize;
    public static Orientation LyricOrientation => Config.IsVertical ? Orientation.Vertical : Orientation.Horizontal;
    public static Color LyricBackground => Config.BackgroundColor;
    public static Color LyricMainTopColor => Config.MainTopColor;
    public static Color LyricMainBottomColor => Config.MainBottomColor;
    public static Color LyricMainBorderColor => Config.MainBorderColor;
    public static Color LyricAltTopColor => Config.AltTopColor;
    public static Color LyricAltBottomColor => Config.AltBottomColor;
    public static Color LyricAltBorderColor => Config.AltBorderColor;

    public static void UpdateLyrics() { }

    public static void SyncCurrentLyrics() { }
    public static string CurrentMainLyric { get; set; } = string.Empty;
    public static string CurrentAltLyric { get; set; } = string.Empty;
}