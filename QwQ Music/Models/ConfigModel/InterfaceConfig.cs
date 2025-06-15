using System.Text.Json.Serialization;
using QwQ_Music.Services;

namespace QwQ_Music.Models.ConfigModel;

public class InterfaceConfig
{
    public bool IsOpenCoverConfig { set; get; }

    public ColorExtractionAlgorithm SelectedColorExtractionAlgorithm { get; set; } = ColorExtractionAlgorithm.KMeans;
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(InterfaceConfig))]
internal partial class InterfaceConfigJsonSerializerContext : JsonSerializerContext;
