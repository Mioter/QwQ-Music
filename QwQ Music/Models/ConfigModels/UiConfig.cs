using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using QwQ_Music.Common;
using QwQ_Music.Common.Services;

namespace QwQ_Music.Models.ConfigModels;

public class UiConfig {
    public VisualConfig VisualConfig { get; set; } = new();

    public ThemeConfig ThemeConfig { get; set; } = new();

    public StyleConfig StyleConfig { get; set; } = new();

    public SpectrumConfig SpectrumConfig { get; set; } = new();
}

public partial class VisualConfig : ObservableObject {
    [ObservableProperty]
    public partial bool IsStaticBackground { get; set; }

    [ObservableProperty]
    public partial bool ColorfulUi { get; set; }

    public bool AllowNonSquareCover { get; set; }

    public bool IgnoreWhite { get; set; } = true;

    public bool ToLab { get; set; } = true;

    public bool UseKMeansPp { get; set; } = true;

    [ObservableProperty]
    public partial ColorExtractionAlgorithm SelectedColorExtractionAlgorithm { get; set; } =
        ColorExtractionAlgorithm.KMeans;
}

public partial class SpectrumConfig : ObservableObject {
    public bool IsEnabled { get; set; } = true;

    [ObservableProperty]
    public partial double LineThickness { get; set; } = 2d;

    [ObservableProperty]
    public partial double AmplitudeScale { get; set; } = 1d;

    [ObservableProperty]
    public partial double SmoothingFactor { get; set; } = 0.15d;

    public int FftSizeIndex {
        get;
        set {
            if (value == field)
                return;

            field = value;
            OnPropertyChanged(nameof(FftSize));
        }
    } = 11;

    [JsonIgnore]
    public int FftSize => (int)Math.Pow(2, FftSizeIndex);

    public int UpdateIntervalMs { get; set; } = 100;
}

public partial class ThemeConfig : ObservableObject {
    [ObservableProperty]
    public partial string CurrentFont { get; set; } = AppResources.CUTE_FONT_KEY;

    [ObservableProperty]
    public partial string Theme { get; set; } = "Default";
}

public class StyleConfig : ObservableObject {
    public bool[] AlbumCard { get; set; } = [true, false, false];
}