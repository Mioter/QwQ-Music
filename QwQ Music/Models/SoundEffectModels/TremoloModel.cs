using QwQ_Music.Models.ModelBase;

namespace QwQ_Music.Models.SoundEffectModels;

public class TremoloModel() : EffectModelBase("Tremolo")
{
    private float _frequencyHz = 5f;
    public float FrequencyHz
    {
        get => _frequencyHz;
        set
        {
            if (SetProperty(ref _frequencyHz, value))
            {
                UpdateParameter(nameof(FrequencyHz), _frequencyHz);
            }
        }
    }

    private float _depth = 0.5f;
    public float Depth
    {
        get => _depth;
        set
        {
            if (SetProperty(ref _depth, value))
            {
                UpdateParameter(nameof(Depth), _depth);
            }
        }
    }

    protected override void UpdateAllParameter()
    {
        base.UpdateAllParameter();
        UpdateParameter(nameof(FrequencyHz), _frequencyHz);
        UpdateParameter(nameof(Depth), _depth);
    }
}
