using CommunityToolkit.Mvvm.ComponentModel;

namespace QwQ_Music.Models;

public partial class ParameterStruct<T>(T value, T maxValue, T minValue) : ObservableObject
{
    [ObservableProperty]
    public partial T Value { get; set; } = value;

    public T MaxValue => maxValue;

    public T MinValue => minValue;
}
