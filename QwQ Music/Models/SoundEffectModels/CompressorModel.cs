using QwQ_Music.Models.ModelBase;

namespace QwQ_Music.Models.SoundEffectModels;

public class CompressorModel() : EffectModelBase("Compressor")
{
    private float _threshold = -20.0f;
    public float Threshold
    {
        get => _threshold;
        set
        {
            if (SetProperty(ref _threshold, value))
            {
                UpdateParameter(nameof(Threshold), _threshold);
            }
        }
    }

    private float _ratio = 4.0f;
    public float Ratio
    {
        get => _ratio;
        set
        {
            if (SetProperty(ref _ratio, value))
            {
                UpdateParameter(nameof(Ratio), _ratio);
            }
        }
    }

    private float _attackMs = 10.0f;
    public float AttackMs
    {
        get => _attackMs;
        set
        {
            if (SetProperty(ref _attackMs, value))
            {
                UpdateParameter(nameof(AttackMs), _attackMs);
            }
        }
    }

    private float _releaseMs = 100.0f;
    public float ReleaseMs
    {
        get => _releaseMs;
        set
        {
            if (SetProperty(ref _releaseMs, value))
            {
                UpdateParameter(nameof(ReleaseMs), _releaseMs);
            }
        }
    }

    protected override void UpdateAllParameter()
    {
        base.UpdateAllParameter();
        UpdateParameter(nameof(Threshold), _threshold);
        UpdateParameter(nameof(Ratio), _ratio);
        UpdateParameter(nameof(AttackMs), _attackMs);
        UpdateParameter(nameof(ReleaseMs), _releaseMs);
    }
}
