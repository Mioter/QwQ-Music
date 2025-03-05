namespace QwQ_Music.Converter;

public static class UiStateConverter
{
    public static PriorityBoolMultiConverter PriorityBool { get; } = new();

    public static WindowStateToBoolConverter WindowStateToBool { get; } = new();
}
