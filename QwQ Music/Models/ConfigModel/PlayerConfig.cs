using System.IO;
using System.Text.Json.Serialization;
using QwQ_Music.Utilities;

namespace QwQ_Music.Models.ConfigModel;

public class PlayerConfig
{
    public int Volume { get; set; }

    public bool IsMuted { get; set; }

    public static string LatestPlayListName => string.Empty;

    public static string CoverSavePath =>
        EnsureExists.Path(Path.Combine(Directory.GetCurrentDirectory(), "cache", "cover"));

    public static string LyricsSavePath =>
        EnsureExists.Path(Path.Combine(Directory.GetCurrentDirectory(), "cache", "lyrics"));
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(PlayerConfig))]
internal partial class PlayerConfigJsonSerializerContext : JsonSerializerContext;
