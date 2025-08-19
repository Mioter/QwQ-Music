namespace QwQ_Music.Common.Audio.SoundModifier;

/// <summary>
///     回放增益效果器
/// </summary>
public class ReplayGainModifier : SoundFlow.Abstracts.SoundModifier
{
    /// <summary>
    ///     回放增益值。
    /// </summary>
    public float Gain
    {
        get;
        set
        {
            if (value <= 0)
                return;

            field = value;
        }
    } = 1.0f; // Default gain

    /// <inheritdoc />
    public override string Name { get; set; } = "Replay Gain";

    /// <inheritdoc />
    public override float ProcessSample(float sample, int channel)
    {
        return sample * Gain;
    }
}
