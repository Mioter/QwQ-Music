using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace QwQ_Music.Models.ModelBase;

public abstract class EffectModelBase(string effectName) : ObservableObject
{
    private bool _isEnabled;
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (SetProperty(ref _isEnabled, value))
            {
                UpdateParameter("Enabled", _isEnabled);
            }
        }
    }

    public static event Action<(string, string, object)>? ParameterChanged;

    protected void UpdateParameter(string parameter, object value) => UpdateEffectConfig(effectName, parameter, value);

    protected static void UpdateEffectConfig(string effectName, string parameter, object value) =>
        ParameterChanged?.Invoke((effectName, parameter, value));

    protected virtual void UpdateAllParameter()
    {
        UpdateParameter("Enabled", IsEnabled);
    }
}
