using Avalonia.Media;
using QwQ_Music.Helper;
using QwQ_Music.Models;
using QwQ_Music.Models.ConfigModel;
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

    public static DesktopLyricConfig DesktopLyric { get; } = ConfigInfoModel.LyricConfig.DesktopLyric;

    public static RolledLyricsConfig RolledLyricsConfig { get; } = ConfigInfoModel.LyricConfig.RolledLyricsConfig;

    public static TextAlignment[] TextAlignments { get; } = EnumHelper<TextAlignment>.ToArray();
}
