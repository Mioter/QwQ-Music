using CommunityToolkit.Mvvm.ComponentModel;
using QwQ_Music.Definitions.Enums;

namespace QwQ_Music.Models.ConfigModels;

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

    public string LatestPlayListName { get; set; } = string.Empty;

    public static int[] AudioOutputSampleRateArray { get; } =
        [44100, 48000, 88200, 96000, 176400, 192000, 352800, 384000];

    public int SampleRate { get; set; } = AudioOutputSampleRateArray[1];

    /// <summary>
    /// 播放模式
    /// </summary>
    [ObservableProperty]
    public partial PlayMode PlayMode { get; set; } = PlayMode.Sequential;
}
