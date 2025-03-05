using Avalonia.Media;
using QwQ_Music.Models;
using static QwQ_Music.Models.LanguageModel;

namespace QwQ_Music.ViewModels;

public class LyricConfigPageViewModel : ViewModelBase
{
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

    public bool LyricIsEnabled
    {
        get => DesktopLyricConfig.IsEnabled;
        set => SetProperty(ref DesktopLyricConfig.IsEnabled, value);
    }

    public int LyricOffset
    {
        get => DesktopLyricConfig.Offset;
        set => SetProperty(ref DesktopLyricConfig.Offset, value);
    }

    public bool LyricIsDoubleLine
    {
        get => DesktopLyricConfig.IsDoubleLine;
        set => SetProperty(ref DesktopLyricConfig.IsDoubleLine, value);
    }

    public bool LyricIsDualLang
    {
        get => DesktopLyricConfig.IsDualLang;
        set => SetProperty(ref DesktopLyricConfig.IsDualLang, value);
    }

    public bool LyricIsVertical
    {
        get => DesktopLyricConfig.IsVertical;
        set => SetProperty(ref DesktopLyricConfig.IsVertical, value);
    }

    public int LyricPositionX
    {
        get => DesktopLyricConfig.Position.X;
        set => SetProperty(ref DesktopLyricConfig.Position, new(value, DesktopLyricConfig.Position.Y));
    }

    public int LyricPositionY
    {
        get => DesktopLyricConfig.Position.Y;
        set => SetProperty(ref DesktopLyricConfig.Position, new(DesktopLyricConfig.Position.X, value));
    }

    public int LyricWidth
    {
        get => (int)DesktopLyricConfig.Size.Width;
        set => SetProperty(ref DesktopLyricConfig.Size, new(value, DesktopLyricConfig.Size.Height));
    }

    public int LyricHeight
    {
        get => (int)DesktopLyricConfig.Size.Height;
        set => SetProperty(ref DesktopLyricConfig.Size, new(DesktopLyricConfig.Size.Width, value));
    }

    public Color LyricMainTopColor
    {
        get => DesktopLyricConfig.MainTopColor;
        set => SetProperty(ref DesktopLyricConfig.MainTopColor, value);
    }

    public Color LyricMainBottomColor
    {
        get => DesktopLyricConfig.MainBottomColor;
        set => SetProperty(ref DesktopLyricConfig.MainBottomColor, value);
    }

    public Color LyricMainBorderColor
    {
        get => DesktopLyricConfig.MainBorderColor;
        set => SetProperty(ref DesktopLyricConfig.MainBorderColor, value);
    }

    public Color LyricAltTopColor
    {
        get => DesktopLyricConfig.AltTopColor;
        set => SetProperty(ref DesktopLyricConfig.AltTopColor, value);
    }

    public Color LyricAltBottomColor
    {
        get => DesktopLyricConfig.AltBottomColor;
        set => SetProperty(ref DesktopLyricConfig.AltBottomColor, value);
    }

    public Color LyricAltBorderColor
    {
        get => DesktopLyricConfig.AltBorderColor;
        set => SetProperty(ref DesktopLyricConfig.AltBorderColor, value);
    }
}
