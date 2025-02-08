using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

namespace QwQ_Music.CustomControls;

public class SwitchControl : ContentControl
{
    public static readonly StyledProperty<bool> ConditionProperty =
        AvaloniaProperty.Register<SwitchControl, bool>(
            nameof(Condition),
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<object> TrueContentProperty =
        AvaloniaProperty.Register<SwitchControl, object>(
            nameof(TrueContent),
            inherits: true); // 启用属性继承

    public static readonly StyledProperty<object> FalseContentProperty =
        AvaloniaProperty.Register<SwitchControl, object>(
            nameof(FalseContent),
            inherits: true); // 启用属性继承

    static SwitchControl()
    {
        ConditionProperty.Changed.AddClassHandler<SwitchControl>((x, _) => x.UpdateContent());
        TrueContentProperty.Changed.AddClassHandler<SwitchControl>((x, _) => x.UpdateContent());
        FalseContentProperty.Changed.AddClassHandler<SwitchControl>((x, _) => x.UpdateContent());
    }

    public bool Condition
    {
        get => GetValue(ConditionProperty);
        set => SetValue(ConditionProperty, value);
    }

    public object TrueContent
    {
        get => GetValue(TrueContentProperty);
        set => SetValue(TrueContentProperty, value);
    }

    public object FalseContent
    {
        get => GetValue(FalseContentProperty);
        set => SetValue(FalseContentProperty, value);
    }

    private void UpdateContent()
    {
        // 自动继承父级的 TrueContent/FalseContent（若未显式设置）
        Content = Condition ? TrueContent : FalseContent;
    }
}
