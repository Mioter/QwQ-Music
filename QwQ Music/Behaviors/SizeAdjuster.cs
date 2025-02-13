using System;
using Avalonia;
using Avalonia.Controls;

namespace QwQ_Music.Behaviors;

public class SizeAdjuster
{
    // 定义附加属性
    public static readonly AttachedProperty<bool> IsAdjustedProperty =
        AvaloniaProperty.RegisterAttached<SizeAdjuster, Control, bool>("IsAdjusted");

    public static readonly AttachedProperty<string> ScaleFactorProperty =
        AvaloniaProperty.RegisterAttached<SizeAdjuster, Control, string>("ScaleFactor", "1");

    public static readonly AttachedProperty<bool> CanAdjustProperty =
        AvaloniaProperty.RegisterAttached<SizeAdjuster, Control, bool>("CanAdjust", true);

    public static readonly AttachedProperty<Size?> OriginalSizeProperty =
        AvaloniaProperty.RegisterAttached<SizeAdjuster, Control, Size?>("OriginalSize");

    static SizeAdjuster()
    {
        IsAdjustedProperty.Changed.Subscribe(OnPropertyChanged);
        CanAdjustProperty.Changed.Subscribe(OnPropertyChanged);
    }

    // Getter 和 Setter 方法
    public static bool GetIsAdjusted(Control element)
    {
        return element.GetValue(IsAdjustedProperty);
    }

    public static void SetIsAdjusted(Control element, bool value)
    {
        element.SetValue(IsAdjustedProperty, value);
    }

    public static string GetScaleFactor(Control element)
    {
        return element.GetValue(ScaleFactorProperty);
    }

    public static void SetScaleFactor(Control element, string value)
    {
        element.SetValue(ScaleFactorProperty, value);
    }

    public static bool GetCanAdjust(Control element)
    {
        return element.GetValue(CanAdjustProperty);
    }

    public static void SetCanAdjust(Control element, bool value)
    {
        element.SetValue(CanAdjustProperty, value);
    }

    public static Size? GetOriginalSize(Control element)
    {
        return element.GetValue(OriginalSizeProperty);
    }

    public static void SetOriginalSize(Control element, Size? value)
    {
        element.SetValue(OriginalSizeProperty, value);
    }

    // 属性更改事件处理
    private static void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Sender is not Control control || !GetCanAdjust(control)) return;

        if (GetIsAdjusted(control))
        {
            ApplyScaling(control, ParseScaleFactor(GetScaleFactor(control)));
        }
        else
        {
            RestoreOriginalSize(control);
        }
    }

    // 应用缩放
    private static void ApplyScaling(Control control, (double WidthScale, double HeightScale) scaleFactor)
    {
        var originalSize = GetOriginalSize(control) ?? new Size(control.Width, control.Height);
        SetOriginalSize(control, originalSize);

        control.Width = originalSize.Width * scaleFactor.WidthScale;
        control.Height = originalSize.Height * scaleFactor.HeightScale;
    }

    // 恢复原始尺寸
    private static void RestoreOriginalSize(Control control)
    {
        var originalSize = GetOriginalSize(control);
        if (originalSize == null) return;

        control.Width = originalSize.Value.Width;
        control.Height = originalSize.Value.Height;
        SetOriginalSize(control, null); // 清除存储的原始尺寸
    }

    // 解析缩放因子
    private static (double WidthScale, double HeightScale) ParseScaleFactor(string scaleFactorString)
    {
        try
        {
            string[] parts = scaleFactorString.Split(',');

            return parts.Length switch
            {
                1 => (double.Parse(parts[0]), double.Parse(parts[0])),
                2 => (double.Parse(parts[0]), double.Parse(parts[1])),
                _ => throw new ArgumentException("Invalid format for ScaleFactor. Expected a single number or two numbers separated by a comma."),
            };
        }
        catch (Exception ex)
        {
            throw new FormatException("Failed to parse ScaleFactor.", ex);
        }
    }
}
