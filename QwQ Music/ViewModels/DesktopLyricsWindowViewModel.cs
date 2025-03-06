using System.Collections.Generic;
using Avalonia.Layout;
using QwQ_Music.Models;
using QwQ_Music.Models.ConfigModel;

namespace QwQ_Music.ViewModels;

public static class DesktopLyricsWindowViewModel
{
    public static Dictionary<int, string[]>? Lyrics { get; set; }

    public static DesktopLyricConfig Config => ConfigInfoModel.DesktopLyricConfig;

    public static Orientation LyricOrientation =>
        Config.LyricIsVertical ? Orientation.Vertical : Orientation.Horizontal;

    public static void UpdateLyrics() { }

    public static void SyncCurrentLyrics() { }

    public static string CurrentMainLyric { get; set; } = string.Empty;
    public static string CurrentAltLyric { get; set; } = string.Empty;
}
