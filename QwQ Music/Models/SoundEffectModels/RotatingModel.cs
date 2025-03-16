using QwQ_Music.Models.ModelBase;

namespace QwQ_Music.Models.SoundEffectModels;

public class RotatingModel() : EffectModelBase("Rotating")
{
    private float _rotationSpeed = 1f;
    public float RotationSpeed
    {
        get => _rotationSpeed;
        set
        {
            if (SetProperty(ref _rotationSpeed, value))
            {
                UpdateParameter(nameof(RotationSpeed), _rotationSpeed);
            }
        }
    }

    private bool _isClockwise = true;
    public bool IsClockwise
    {
        get => _isClockwise;
        set
        {
            if (SetProperty(ref _isClockwise, value))
            {
                UpdateParameter(nameof(IsClockwise), _isClockwise);
            }
        }
    }

    private float _radius = 50f;
    public float Radius
    {
        get => _radius;
        set
        {
            if (SetProperty(ref _radius, value))
            {
                UpdateParameter(nameof(Radius), _radius);
            }
        }
    }

    protected override void UpdateAllParameter()
    {
        base.UpdateAllParameter();
        UpdateParameter(nameof(RotationSpeed), RotationSpeed);
        UpdateParameter(nameof(IsClockwise), IsClockwise);
        UpdateParameter(nameof(Radius), Radius);
    }
}
