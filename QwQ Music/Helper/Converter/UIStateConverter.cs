namespace QwQ_Music.Helper.Converter;

public static class UiStateConverter
{
    public static PriorityBoolMultiConverter PriorityBool { get; } = new();

    public static WindowStateToBoolConverter WindowStateToBool { get; } = new();
}
