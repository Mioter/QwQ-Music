using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using QwQ_Music.Definitions.Enums;

namespace QwQ_Music.Models.ConfigModel;

public partial class SystemConfig : ObservableObject
{
    public bool IsExpandedProgramBehavior { get; set; }

    [ObservableProperty]
    public partial ClosingBehavior ClosingBehavior { get; set; } = ClosingBehavior.AskAbout;
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(SystemConfig))]
internal partial class SystemConfigJsonSerializerContext : JsonSerializerContext;
