using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Controls;
using QwQ_Music.Models.ModelBase;

namespace QwQ_Music.Models.SoundEffectModels;

public partial class SpatialModel() : EffectModelBase("Spatial")
{
    private float _angle;
    public float Angle
    {
        get => _angle;
        set
        {
            if (SetProperty(ref _angle, value))
            {
                UpdateParameter(nameof(Angle), _angle);
            }
        }
    }

    private float _distance;
    public float Distance
    {
        get => _distance;
        set
        {
            if (SetProperty(ref _distance, value))
            {
                UpdateParameter(nameof(Distance), _distance);
            }
        }
    }

    [RelayCommand]
    private void OnSpeakerPositionChanged(PositionChangedEventArgs e)
    {
        Angle = (float)e.Angle;
        Distance = (float)e.Distance;
    }

    protected override void UpdateAllParameter()
    {
        base.UpdateAllParameter();
        UpdateParameter(nameof(Angle), Angle);
        UpdateParameter(nameof(Distance), Distance);
    }
}
