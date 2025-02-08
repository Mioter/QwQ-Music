using System;
using Avalonia;
using Avalonia.Controls;

namespace QwQ_Music.Attribute;

public class RoundedCornerBehavior
{
    // 定义附加属性 IsEnabled
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<RoundedCornerBehavior, Control, bool>(
            "IsEnabled",
            false,
            true);

    static RoundedCornerBehavior()
    {
        IsEnabledProperty.Changed.Subscribe(OnIsEnabledChanged);
    }

    // 获取附加属性值
    public static bool GetIsEnabled(Control control)
    {
        return control.GetValue(IsEnabledProperty);
    }

    // 设置附加属性值
    public static void SetIsEnabled(Control control, bool value)
    {
        control.SetValue(IsEnabledProperty, value);
    }

    // 附加属性改变时触发的方法
    private static void OnIsEnabledChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Sender is not Control control || e.NewValue is not bool) return;

        control.LayoutUpdated += (_, _) =>
        {
            double width = control.Bounds.Width;
            double height = control.Bounds.Height;
            double radius = Math.Min(width, height) / 2.0;

            switch (control)
            {
                case Border border:
                    border.CornerRadius = new CornerRadius(radius);
                    break;
                case Button button:
                    button.CornerRadius = new CornerRadius(radius);
                    break;
            }
        };
    }
}
