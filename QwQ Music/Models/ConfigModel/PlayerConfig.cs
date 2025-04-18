using System.IO;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using QwQ_Music.Enums;
using QwQ_Music.Utilities;

namespace QwQ_Music.Models.ConfigModel;

public partial class PlayerConfig : ObservableObject
{
    public int Volume { get; set; } = 100;

    public bool IsMuted { get; set; }

    public float PlaybackSpeed { get; set; } = 1.0f;

    public bool AutoSwitchNext { get; set; } = true;

    public bool IsRestartPlay { get; set; } = true;

    public bool IsRealRandom { get; set; }

    [ObservableProperty]
    public partial bool IsAutoSetSampleRate { get; set; } = true;

    public string LatestPlayListName { get; set; } = "默认播放列表";

    public static int[] AudioOutputSampleRateArray { get; } =
        [44100, 48000, 88200, 96000, 176400, 192000, 352800, 384000];

    public int SampleRate { get; set; } = AudioOutputSampleRateArray[1];

    public static string CoverSavePath =>
        PathEnsurer.EnsureDirectoryExists(Path.Combine(Directory.GetCurrentDirectory(), "cache", "cover"));

    public static string LyricsSavePath =>
        PathEnsurer.EnsureDirectoryExists(Path.Combine(Directory.GetCurrentDirectory(), "cache", "lyrics"));

    /// <summary>
    /// 播放模式
    /// </summary>
    [ObservableProperty]
    public partial PlayMode PlayMode { get; set; } = PlayMode.Sequential;
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(PlayerConfig))]
internal partial class PlayerConfigJsonSerializerContext : JsonSerializerContext;
