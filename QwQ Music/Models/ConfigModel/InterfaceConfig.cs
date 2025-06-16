using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using QwQ_Music.Services;

namespace QwQ_Music.Models.ConfigModel;

public class InterfaceConfig
{
    public bool IsOpenCoverConfig { set; get; }

    public string? Skin { get; set; }

    public bool FollowSystemTheme { get; set; }

    public CoverConfig CoverConfig { get; set; } = new();
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

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(InterfaceConfig))]
internal partial class InterfaceConfigJsonSerializerContext : JsonSerializerContext;
