using QwQ_Music.Models.ModelBase;

namespace QwQ_Music.Models.SoundEffectModels;

public class StereoEnhancementModel() : EffectModelBase("Stereo Enhancement")
{
    private float _enhancementFactor = 1.5f;
    public float EnhancementFactor
    {
        get => _enhancementFactor;
        set
        {
            if (SetProperty(ref _enhancementFactor, value))
            {
                UpdateParameter(nameof(EnhancementFactor), _enhancementFactor);
            }
        }
    }

    private float _dynamicRangeCompression = 0.5f;
    public float DynamicRangeCompression
    {
        get => _dynamicRangeCompression;
        set
        {
            if (SetProperty(ref _dynamicRangeCompression, value))
            {
                UpdateParameter(nameof(DynamicRangeCompression), _dynamicRangeCompression);
            }
        }
    }

    private bool _bassMixing;
    public bool BassMixing
    {
        get => _bassMixing;
        set
        {
            if (SetProperty(ref _bassMixing, value))
            {
                UpdateParameter(nameof(BassMixing), _bassMixing);
            }
        }
    }

    private float _stereoWidth = 1.0f;
    public float StereoWidth
    {
        get => _stereoWidth;
        set
        {
            if (SetProperty(ref _stereoWidth, value))
            {
                UpdateParameter(nameof(StereoWidth), _stereoWidth);
            }
        }
    }

    private float _highFrequencyBoost = 1.0f;
    public float HighFrequencyBoost
    {
        get => _highFrequencyBoost;
        set
        {
            if (SetProperty(ref _highFrequencyBoost, value))
            {
                UpdateParameter(nameof(HighFrequencyBoost), _highFrequencyBoost);
            }
        }
    }

    protected override void UpdateAllParameter()
    {
        base.UpdateAllParameter();
        UpdateParameter(nameof(EnhancementFactor), EnhancementFactor);
        UpdateParameter(nameof(DynamicRangeCompression), DynamicRangeCompression);
        UpdateParameter(nameof(BassMixing), BassMixing);
        UpdateParameter(nameof(StereoWidth), StereoWidth);
        UpdateParameter(nameof(HighFrequencyBoost), HighFrequencyBoost);
    }
}
