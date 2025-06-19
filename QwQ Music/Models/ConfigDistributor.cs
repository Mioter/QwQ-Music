using System.Text.Json.Serialization;
using QwQ_Music.Models.ConfigModels;

namespace QwQ_Music.Models;

public class UserConfig
{
    public InterfaceConfig InterfaceConfig { get; set; } = new();

    public PlayerConfig PlayerConfig { get; set; } = new();

    public LyricConfig LyricConfig { get; set; } = new();

    public AudioModifierConfig AudioModifierConfig { get; set; } = new();

    public SystemConfig SystemConfig { get; set; } = new();

    public HotkeyConfig HotkeyConfig { get; set; } = new();
}

public class ServiceConfig
{
    public JsonServiceConfig JsonServiceConfig { get; set; } = new();

    public LoggerServiceConfig LoggerServiceConfig { get; set; } = new();

    public DataBaseConfig DataBaseConfig { get; set; } = new();
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(UserConfig))]
internal partial class UserConfigJsonSerializerContext : JsonSerializerContext;
