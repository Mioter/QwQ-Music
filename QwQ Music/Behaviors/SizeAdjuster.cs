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

    // 监听 IsAdjusted 属性的变化
    static SizeAdjuster()
    {
        IsAdjustedProperty.Changed.Subscribe(OnPropertyChanged);
    }

    // 获取附加属性值
    public static bool GetIsAdjusted(Control element)
    {
        return element.GetValue(IsAdjustedProperty);
    }

    public static string GetScaleFactor(Control element)
    {
        return element.GetValue(ScaleFactorProperty);
    }

    // 设置附加属性值
    public static void SetIsAdjusted(Control element, bool value)
    {
        element.SetValue(IsAdjustedProperty, value);
    }

    public static void SetScaleFactor(Control element, string value)
    {
        element.SetValue(ScaleFactorProperty, value);
    }

    private static void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Sender is not Control control) return;

        // 如果 IsAdjusted 为 true，按比例缩放控件的宽高
        if (GetIsAdjusted(control))
        {
            var scaleFactor = ParseScaleFactor(GetScaleFactor(control));
            ApplyScaling(control, scaleFactor);
        }
        else
        {
            // 如果 IsAdjusted 为 false，恢复控件的原始宽高
            RestoreOriginalSize(control);
        }
    }

    private static void ApplyScaling(Control control, (double WidthScale, double HeightScale) scaleFactor)
    {
        control.Tag ??= new Size(control.Width, control.Height);

        // 根据比例缩放控件的宽高
        control.Width *= scaleFactor.WidthScale;
        control.Height *= scaleFactor.HeightScale;
    }

    private static void RestoreOriginalSize(Control control)
    {
        if (control.Tag is not Size originalSize) return;

        // 恢复宽度和高度
        control.Width = originalSize.Width;
        control.Height = originalSize.Height;
        control.Tag = null; // 清除存储的原始尺寸
    }

    private static (double WidthScale, double HeightScale) ParseScaleFactor(string scaleFactorString)
    {
        string[] parts = scaleFactorString.Split(',');
        switch (parts.Length)
        {
            case 1:
                {
                    double scale = double.Parse(parts[0]);
                    return (scale, scale);
                }
            case 2:
                {
                    double widthScale = double.Parse(parts[0]);
                    double heightScale = double.Parse(parts[1]);
                    return (widthScale, heightScale);
                }
            default:
                throw new ArgumentException("Invalid format for ScaleFactor. Expected a single number or two numbers separated by a comma.");
        }
    }
}
