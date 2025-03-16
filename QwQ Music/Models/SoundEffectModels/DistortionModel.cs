using QwQ_Music.Models.ModelBase;

namespace QwQ_Music.Models.SoundEffectModels;

public class DistortionModel() : EffectModelBase("Distortion")
{
    private float _drive = 10f;
    public float Drive
    {
        get => _drive;
        set
        {
            if (SetProperty(ref _drive, value))
            {
                UpdateParameter(nameof(Drive), _drive);
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
        UpdateParameter(nameof(Drive), _drive);
        UpdateParameter(nameof(Mix), _mix);
    }
}
