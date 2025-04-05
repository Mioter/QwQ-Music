using SoundFlow.Abstracts;

namespace SoundFlow.Modifiers;

/// <summary>
/// 回放增益效果器
/// </summary>
public class ReplayGainModifier : SoundModifier
{
    /// <summary>
    /// 回放增益值。
    /// </summary>
    public float Gain { get; set; } = 1.0f; // Default gain

    /// <inheritdoc />
    public override string Name { get; set; } = "Replay Gain";

    /// <inheritdoc />
    public override float ProcessSample(float sample, int channel)
    {
        return sample * Gain;
    }
}
