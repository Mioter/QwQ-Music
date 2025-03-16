using QwQ_Music.Models.ModelBase;

namespace QwQ_Music.Models.SoundEffectModels;

public class DelayModel() : EffectModelBase("Delay")
{
    private float _delayMs = 200f;
    public float DelayMs
    {
        get => _delayMs;
        set
        {
            if (SetProperty(ref _delayMs, value))
            {
                UpdateParameter(nameof(DelayMs), _delayMs);
            }
        }
    }

    private float _feedback = 0.5f;
    public float Feedback
    {
        get => _feedback;
        set
        {
            if (SetProperty(ref _feedback, value))
            {
                UpdateParameter(nameof(Feedback), _feedback);
            }
        }
    }

    private float _mix = 0.5f;
    public float Mix
    {
        get => _mix;
        set
        {
            if (SetProperty(ref _mix, value))
            {
                UpdateParameter(nameof(Mix), _mix);
            }
        }
    }

    protected override void UpdateAllParameter()
    {
        base.UpdateAllParameter();
        UpdateParameter(nameof(DelayMs), _delayMs);
        UpdateParameter(nameof(Feedback), _feedback);
        UpdateParameter(nameof(Mix), _mix);
    }
}
