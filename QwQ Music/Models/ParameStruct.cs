using CommunityToolkit.Mvvm.ComponentModel;

namespace QwQ_Music.Models;

public class ParameterStruct<T>(T value, T maxValue, T minValue) : ObservableObject
{
    public T Value
    {
        get;
        set => SetProperty(ref field, value);
    } = value;

    public T MaxValue => maxValue;

    public T MinValue => minValue;
}
