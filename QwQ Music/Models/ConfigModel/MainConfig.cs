using System.IO;
using System.Text.Json.Serialization;

namespace QwQ_Music.Models.ConfigModel;

public class MainConfig
{
    public string? Skin { get; set; }

    public bool FollowSystemTheme { get; set; }

    public static string DatabaseSavePath => Path.Combine(Directory.GetCurrentDirectory(), "config", "data.db");
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(MainConfig))]
internal partial class MainConfigJsonSerializerContext : JsonSerializerContext;
