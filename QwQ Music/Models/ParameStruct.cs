using CommunityToolkit.Mvvm.ComponentModel;

namespace QwQ_Music.Models;

public class ParameterStruct<T>(T value, T maxValue, T minValue) : ObservableObject
{
    private T _value = value;
    public T Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }

    public T MaxValue => maxValue;

    public T MinValue => minValue;
}
