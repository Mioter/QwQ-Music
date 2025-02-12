using System;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;

namespace QwQ_Music.Behaviors;

public class DynamicCornerBehavior
{
    static DynamicCornerBehavior()
    {
        IsEnabledProperty.Changed.Subscribe(OnIsEnabledChanged);
        ModeProperty.Changed.Subscribe(OnPropertyChanged);
        AspectRatioProperty.Changed.Subscribe(OnPropertyChanged);
    }

    private static void OnIsEnabledChanged(AvaloniaPropertyChangedEventArgs args)
    {
        if (args.Sender is not Control control) return;

        if ((bool)args.NewValue!)
        {
            control.SizeChanged += OnSizeChanged;
            ApplyCornerRadius(control);
        }
        else
        {
            control.SizeChanged -= OnSizeChanged;
            ResetCornerRadius(control);
        }
    }

    private static void OnPropertyChanged(AvaloniaPropertyChangedEventArgs args)
    {
        if (args.Sender is Control control && GetIsEnabled(control))
        {
            ApplyCornerRadius(control);
        }
    }

    private static void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (sender is Control control && GetIsEnabled(control))
        {
            ApplyCornerRadius(control);
        }
    }

    private static void ApplyCornerRadius(Control control)
    {
        if (!control.IsMeasureValid) return;

        (double width, double height) = GetEffectiveSize(control);
        var cornerRadius = CalculateCornerRadius(control, width, height);

        if (!TryGetCornerRadiusProperty(control, out var property)) return;
        if (property != null) control.SetValue(property, cornerRadius);
    }

    private static (double width, double height) GetEffectiveSize(Control control)
    {
        return control switch
        {
            Layoutable layoutable => (layoutable.Bounds.Width, layoutable.Bounds.Height),
            _ => (control.Width, control.Height),
        };
    }

    private static CornerRadius CalculateCornerRadius(Control control, double width, double height)
    {
        var mode = GetMode(control);
        double aspect = GetAspectRatio(control);

        return mode switch
        {
            ShapeMode.Circle => new CornerRadius(Math.Min(width, height) / 2.0),
            ShapeMode.Capsule => new CornerRadius(Math.Min(width, height) / 2.0),
            ShapeMode.Ellipse => new CornerRadius(width * aspect / 2.0, height * aspect / 2.0),
            ShapeMode.Hybrid => new CornerRadius(
                Math.Min(width, height * aspect) / 2.0,
                Math.Min(width * aspect, height) / 2.0),
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    private static bool TryGetCornerRadiusProperty(Control control, out AvaloniaProperty<CornerRadius>? property)
    {
        property = control switch
        {
            Border => Border.CornerRadiusProperty,
            Button => TemplatedControl.CornerRadiusProperty,
            ContentControl => TemplatedControl.CornerRadiusProperty,
            _ => FindCornerRadiusPropertyByReflection(control),
        };
        return property != null;
    }

    private static AvaloniaProperty<CornerRadius>? FindCornerRadiusPropertyByReflection(Control control)
    {
        // 使用缓存机制提升性能
        return control.GetType().GetField("CornerRadiusProperty",
                BindingFlags.Static |
                BindingFlags.Public)?
            .GetValue(null) as AvaloniaProperty<CornerRadius>;
    }

    private static void ResetCornerRadius(Control control)
    {
        if (!TryGetCornerRadiusProperty(control, out var property)) return;
        if (property != null) control.ClearValue(property);
    }
    #region 附加属性

    // 启用行为
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<DynamicCornerBehavior, Control, bool>(
            "IsEnabled",
            false,
            true);

    // 形状模式
    public static readonly AttachedProperty<ShapeMode> ModeProperty =
        AvaloniaProperty.RegisterAttached<DynamicCornerBehavior, Control, ShapeMode>(
            "Mode",
            ShapeMode.Circle,
            true);

    // 自定义比例（用于混合模式）
    public static readonly AttachedProperty<double> AspectRatioProperty =
        AvaloniaProperty.RegisterAttached<DynamicCornerBehavior, Control, double>(
            "AspectRatio",
            1.0,
            true);

    #endregion

    #region 属性访问器

    public static bool GetIsEnabled(Control control)
    {
        return control.GetValue(IsEnabledProperty);
    }

    public static void SetIsEnabled(Control control, bool value)
    {
        control.SetValue(IsEnabledProperty, value);
    }

    public static ShapeMode GetMode(Control control)
    {
        return control.GetValue(ModeProperty);
    }

    public static void SetMode(Control control, ShapeMode value)
    {
        control.SetValue(ModeProperty, value);
    }

    public static double GetAspectRatio(Control control)
    {
        return control.GetValue(AspectRatioProperty);
    }

    public static void SetAspectRatio(Control control, double value)
    {
        control.SetValue(AspectRatioProperty, value);
    }

    #endregion
}
public enum ShapeMode
{
    /// <summary> 正圆形（短边直径） </summary>
    Circle,

    /// <summary> 胶囊形（始终基于高度）</summary>
    Capsule,

    /// <summary> 椭圆（可自定义比例）</summary>
    Ellipse,

    /// <summary> 混合模式（根据方向自适应）</summary>
    Hybrid,
}
