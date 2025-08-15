using CommunityToolkit.Mvvm.ComponentModel;
using QwQ_Music.Common;
using QwQ_Music.Common.Services;

namespace QwQ_Music.Models.ConfigModels;

public class UiConfig
{
    public CoverConfig CoverConfig { get; set; } = new();

    public ThemeConfig ThemeConfig { get; set; } = new();

    public StyleConfig StyleConfig { get; set; } = new();
}

public partial class CoverConfig : ObservableObject
{
    public bool AllowNonSquareCover { get; set; }

    public bool IgnoreWhite { get; set; } = true;

    public bool ToLab { get; set; } = true;

    public bool UseKMeansPp { get; set; } = true;

    [ObservableProperty]
    public partial ColorExtractionAlgorithm SelectedColorExtractionAlgorithm { get; set; } =
        ColorExtractionAlgorithm.KMeans;
}

public partial class ThemeConfig : ObservableObject
{
    [ObservableProperty] public partial string CurrentFont { get; set; } = AppResources.DEFAULT_FONT_KEY;

    [ObservableProperty] public partial string Theme { get; set; } = "Default";
}

public class StyleConfig : ObservableObject
{
    public bool[] AlbumCard { get; set; } = [true, false, false];
}
