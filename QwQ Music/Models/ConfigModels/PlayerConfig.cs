using CommunityToolkit.Mvvm.ComponentModel;
using QwQ_Music.Models.Enums;
using SoundFlow.Backends.MiniAudio.Devices;

namespace QwQ_Music.Models.ConfigModels;

public enum AddMusicBehavior {
    /// <summary>
    ///     添加到下一首
    /// </summary>
    AddToNext,

    /// <summary>
    ///     将播放列表替换为此歌曲
    /// </summary>
    SetToList,

    /// <summary>
    ///     将播放列表替换为此歌曲所在的列表
    /// </summary>
    ReplaceList
}

public partial class PlayerConfig : ObservableObject {
    /// <summary>
    ///     音频设备配置
    /// </summary>
    public MiniAudioDeviceConfig DeviceConfig { get; set; } = new();

    public string? DefaultDevice { get; set; }

    public int Volume { get; set; } = 100;

    public bool IsMuted { get; set; }

    public float PlaybackSpeed { get; set; } = 1.0f;

    public bool AutoSwitchNext { get; set; } = true;

    public bool IsRestartPlay { get; set; } = true;

    [ObservableProperty]
    public partial AddMusicBehavior AddMusicBehavior { get; set; } = AddMusicBehavior.ReplaceList;

    public bool IsRealRandom { get; set; }

    [ObservableProperty]
    public partial bool IsAutoReSample { get; set; } = true;

    public string? LastPlayedFilePath { get; set; }

    [ObservableProperty]
    public partial double CustomMusicReplayGainStandard { get; set; } = -16;

    public static int[] AudioOutputSampleRateArray { get; } = [
        44100, 48000, 88200, 96000, 176400, 192000, 352800, 384000
    ];

    public int SampleRate { get; set; } = AudioOutputSampleRateArray[1];

    /// <summary>
    ///     播放模式
    /// </summary>
    [ObservableProperty]
    public partial PlayMode PlayMode { get; set; } = PlayMode.Sequential;
}