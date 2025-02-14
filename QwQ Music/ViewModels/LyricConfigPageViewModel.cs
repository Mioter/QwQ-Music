using Avalonia.Media;
using Config = QwQ_Music.Models.DesktopLyricConfig;
using static QwQ_Music.Models.LanguageModel;
namespace QwQ_Music.ViewModels;

public class LyricConfigPageViewModel : ViewModelBase {
    

    public static string OffsetName => Lang[nameof(OffsetName)];
    public static string IsEnabledName => Lang[nameof(IsEnabledName)];
    public static string IsDoubleLineName => Lang[nameof(IsDoubleLineName)];
    public static string IsDualLangName => Lang[nameof(IsDualLangName)];
    public static string IsVerticalName => Lang[nameof(IsVerticalName)];
    public static string PositionXName => Lang[nameof(PositionXName)];
    public static string PositionYName => Lang[nameof(PositionYName)];
    public static string WidthName => Lang[nameof(WidthName)];
    public static string HeightName => Lang[nameof(HeightName)];
    public static string MaximizeName => Lang[nameof(MaximizeName)];
    public static string ResetName => Lang[nameof(ResetName)];
    public static string LyricMainTopColorName => Lang[nameof(LyricMainTopColorName)];
    public static string LyricMainBottomColorName => Lang[nameof(LyricMainBottomColorName)];
    public static string LyricMainBorderColorName => Lang[nameof(LyricMainBorderColorName)];
    public static string LyricAltTopColorName => Lang[nameof(LyricAltTopColorName)];
    public static string LyricAltBottomColorName => Lang[nameof(LyricAltBottomColorName)];
    public static string LyricAltBorderColorName => Lang[nameof(LyricAltBorderColorName)];

    public static bool LyricIsEnabled {
        get => Config.IsEnabled;
        set => Config.IsEnabled = value;
    }
    public static int LyricOffset {
        get => Config.Offset;
        set => Config.Offset = value;
    }

    public static bool LyricIsDoubleLine {
        get => Config.IsDoubleLine;
        set => Config.IsDoubleLine = value;
    }

    public static bool LyricIsDualLang {
        get => Config.IsDualLang;
        set => Config.IsDualLang = value;
    }

    public static bool LyricIsVertical {
        get => Config.IsVertical;
        set => Config.IsVertical = value;
    }

    public static int LyricPositionX {
        get => (int)Config.Position.X;
        set => Config.Position = new(value, Config.Position.Y);
    }

    public static int LyricPositionY {
        get => (int)Config.Position.Y;
        set => Config.Position = new(Config.Position.X, value);
    }

    public static int LyricWidth {
        get => (int)Config.Size.Width;
        set => Config.Size = new(value, Config.Size.Height);
    }

    public static int LyricHeight {
        get => (int)Config.Size.Height;
        set => Config.Size = new(Config.Size.Width, value);
    }


    public static Color LyricMainTopColor {
        get => Config.MainTopColor;
        set => Config.MainTopColor = value;
    }

    public static Color LyricMainBottomColor {
        get => Config.MainBottomColor;
        set => Config.MainBottomColor = value;
    }

    public static Color LyricMainBorderColor {
        get => Config.MainBorderColor;
        set => Config.MainBorderColor = value;
    }

    public static Color LyricAltTopColor {
        get => Config.AltTopColor;
        set => Config.AltTopColor = value;
    }

    public static Color LyricAltBottomColor {
        get => Config.AltBottomColor;
        set => Config.AltBottomColor = value;
    }

    public static Color LyricAltBorderColor {
        get => Config.AltBorderColor;
        set => Config.AltBorderColor = value;
    }
}