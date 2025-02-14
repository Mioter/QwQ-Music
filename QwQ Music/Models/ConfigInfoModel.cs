using Avalonia.Controls;
using Color = Avalonia.Media.Color;
using WindowPosition = Avalonia.PixelPoint;
using WindowSize = Avalonia.Size;

namespace QwQ_Music.Models;

public class ConfigInfoModel {
    public PlayerConfig? PlayerConfig { get; init; }
}

public class PlayerConfig {
    public int Volume { get; init; }
}

public static class MainConfig {
    public static string Skin = "Light";
    public static bool FollowSystemTheme;
}

internal static class DesktopLyricConfig {
    public static bool IsEnabled { get; set; }
    public static bool IsDoubleLine { get; set; }
    public static bool IsDualLang { get; set; }
    public static bool IsVertical { get; set; }
    public static int Offset { get; set; }
    public static int MainFontSize { get; set; }
    public static int AltFontSize { get; set; }
    public static Color MainTopColor { get; set; }
    public static Color MainBottomColor { get; set; }
    public static Color MainBorderColor { get; set; }
    public static Color AltTopColor { get; set; }
    public static Color AltBottomColor { get; set; }
    public static Color AltBorderColor { get; set; }
    public static Color BackgroundColor { get; set; }
    public static WindowPosition Position { get; set; }
    public static WindowSize Size { get; set; }

    public static void ResetAll() {
        IsEnabled = true;
        IsDoubleLine = false;
        IsDualLang = false;
        IsVertical = false;
        Offset = 0;
        MainTopColor = Color.Parse("#FF0000FF");
        MainBottomColor = Color.Parse("#FF0000FF");
        MainBorderColor = Color.Parse("#FF0000FF");
        AltTopColor = Color.Parse("#FF0000FF");
        AltBottomColor = Color.Parse("#FF0000FF");
        AltBorderColor = Color.Parse("#FF0000FF");
        BackgroundColor = Color.Parse("#000000FF");
        Position = new(0, 800);
        Size = new(800, 200);
        MainFontSize = 40;
        AltFontSize = 30;
    }
}