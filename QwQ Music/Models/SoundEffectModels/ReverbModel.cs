using QwQ_Music.Models.ModelBase;

namespace QwQ_Music.Models.SoundEffectModels;

public class ReverbModel() : EffectModelBase("Reverb")
{
    private float _roomSize = 1.0f;
    public float RoomSize
    {
        get => _roomSize;
        set
        {
            if (SetProperty(ref _roomSize, value))
            {
                UpdateParameter(nameof(RoomSize), _roomSize);
            }
        }
    }

    private float _dryMix = 0.5f;
    public float DryMix
    {
        get => _dryMix;
        set
        {
            if (SetProperty(ref _dryMix, value))
            {
                UpdateParameter(nameof(DryMix), _dryMix);
            }
        }
    }

    private float _wetMix = 0.5f;
    public float WetMix
    {
        get => _wetMix;
        set
        {
            if (SetProperty(ref _wetMix, value))
            {
                UpdateParameter(nameof(WetMix), _wetMix);
            }
        }
    }

    private float _dampening = 0.5f;
    public float Dampening
    {
        get => _dampening;
        set
        {
            if (SetProperty(ref _dampening, value))
            {
                UpdateParameter(nameof(Dampening), _dampening);
            }
        }
    }

    private float _decayTime = 1.0f;
    public float DecayTime
    {
        get => _decayTime;
        set
        {
            if (SetProperty(ref _decayTime, value))
            {
                UpdateParameter(nameof(DecayTime), _decayTime);
            }
        }
    }

    private float _preDelayMs = 50f;
    public float PreDelayMs
    {
        get => _preDelayMs;
        set
        {
            if (SetProperty(ref _preDelayMs, value))
            {
                UpdateParameter(nameof(PreDelayMs), _preDelayMs);
            }
        }
    }

    protected override void UpdateAllParameter()
    {
        base.UpdateAllParameter();
        UpdateParameter(nameof(RoomSize), RoomSize);
        UpdateParameter(nameof(DryMix), DryMix);
        UpdateParameter(nameof(WetMix), WetMix);
        UpdateParameter(nameof(Dampening), Dampening);
        UpdateParameter(nameof(DecayTime), DecayTime);
        UpdateParameter(nameof(PreDelayMs), PreDelayMs);
    }
}
