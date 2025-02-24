using System.Collections.Generic;
using Avalonia.Layout;
using Avalonia.Media;
using QwQ_Music.Models;
using static QwQ_Music.Models.LanguageModel;

namespace QwQ_Music.ViewModels;

public static class DesktopLyricsWindowViewModel {
    public static Dictionary<int, string[]>? Lyrics { get; set; }

    public static int LyricMainFontSize => DesktopLyricConfig.MainFontSize;
    public static int LyricAltFontSize => DesktopLyricConfig.AltFontSize;
    public static Orientation LyricOrientation => DesktopLyricConfig.IsVertical ? Orientation.Vertical : Orientation.Horizontal;
    public static Color LyricBackground => DesktopLyricConfig.BackgroundColor;
    public static Color LyricMainTopColor => DesktopLyricConfig.MainTopColor;
    public static Color LyricMainBottomColor => DesktopLyricConfig.MainBottomColor;
    public static Color LyricMainBorderColor => DesktopLyricConfig.MainBorderColor;
    public static Color LyricAltTopColor => DesktopLyricConfig.AltTopColor;
    public static Color LyricAltBottomColor => DesktopLyricConfig.AltBottomColor;
    public static Color LyricAltBorderColor => DesktopLyricConfig.AltBorderColor;

    public static void UpdateLyrics() { }

    public static void SyncCurrentLyrics() { }
    public static string CurrentMainLyric { get; set; } = string.Empty;
    public static string CurrentAltLyric { get; set; } = string.Empty;
}