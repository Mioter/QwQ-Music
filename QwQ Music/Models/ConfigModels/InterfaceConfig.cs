using CommunityToolkit.Mvvm.ComponentModel;
using QwQ_Music.Services;

namespace QwQ_Music.Models.ConfigModels;

public class InterfaceConfig
{
    public bool IsOpenCoverConfig { set; get; }

    public bool IsOpenThemeConfig { set; get; }

    public CoverConfig CoverConfig { get; set; } = new();

    public ThemeConfig ThemeConfig { get; set; } = new();
}

public partial class CoverConfig : ObservableObject
{
    public bool IgnoreWhite { get; set; } = true;

    public bool ToLab { get; set; } = true;

    public bool UseKMeansPp { get; set; } = true;

    [ObservableProperty]
    public partial ColorExtractionAlgorithm SelectedColorExtractionAlgorithm { get; set; } =
        ColorExtractionAlgorithm.KMeans;
}

public partial class ThemeConfig : ObservableObject
{
    public string? Skin { get; set; }

    [ObservableProperty]
    public partial string LightDarkMode { get; set; } = "Default";
}
