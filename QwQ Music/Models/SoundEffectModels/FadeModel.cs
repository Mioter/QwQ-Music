using QwQ_Music.Models.ModelBase;

namespace QwQ_Music.Models.SoundEffectModels;

public class FadeModel() : EffectModelBase("Fade")
{
    private double _fadeInTimeMs = 1000f;
    public double FadeInTimeMs
    {
        get => _fadeInTimeMs;
        set
        {
            if (SetProperty(ref _fadeInTimeMs, value))
            {
                UpdateParameter(nameof(FadeInTimeMs), _fadeInTimeMs);
            }
        }
    }

    private double _fadeOutTimeMs = 1000f;
    public double FadeOutTimeMs
    {
        get => _fadeOutTimeMs;
        set
        {
            if (SetProperty(ref _fadeOutTimeMs, value))
            {
                UpdateParameter(nameof(FadeOutTimeMs), _fadeOutTimeMs);
            }
        }
    }

    protected override void UpdateAllParameter()
    {
        base.UpdateAllParameter();
        UpdateParameter(nameof(FadeInTimeMs), FadeInTimeMs);
        UpdateParameter(nameof(FadeOutTimeMs), FadeOutTimeMs);
    }
}
