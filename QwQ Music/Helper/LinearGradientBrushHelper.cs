using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace QwQ_Music.Helper;

/// <summary>
/// Border 线性渐变画刷助手，分别支持 BorderBrush 和 Background
/// </summary>
public class BorderLinearGradientBrushHelper
{
    public static readonly AttachedProperty<double> BorderBrushRotateAngleProperty = AvaloniaProperty.RegisterAttached<
        BorderLinearGradientBrushHelper,
        Border,
        double
    >("BorderBrushRotateAngle", coerce: OnBorderBrushRotateAngleChanged);

    public static readonly AttachedProperty<double> BackgroundRotateAngleProperty = AvaloniaProperty.RegisterAttached<
        BorderLinearGradientBrushHelper,
        Border,
        double
    >("BackgroundRotateAngle", coerce: OnBackgroundRotateAngleChanged);

    private static double OnBorderBrushRotateAngleChanged(AvaloniaObject obj, double degrees)
    {
        if (obj is Border { BorderBrush: LinearGradientBrush brush } border)
        {
            LinearGradientBrushHelperCore.SetGradientRotation(border, brush, degrees);
        }
        return degrees;
    }

    private static double OnBackgroundRotateAngleChanged(AvaloniaObject obj, double degrees)
    {
        if (obj is Border { Background: LinearGradientBrush brush } border)
        {
            LinearGradientBrushHelperCore.SetGradientRotation(border, brush, degrees);
        }
        return degrees;
    }

    public static void SetBorderBrushRotateAngle(Border element, double value)
    {
        element.SetValue(BorderBrushRotateAngleProperty, value);
    }

    public static double GetBorderBrushRotateAngle(Border element)
    {
        return element.GetValue(BorderBrushRotateAngleProperty);
    }

    public static void SetBackgroundRotateAngle(Border element, double value)
    {
        element.SetValue(BackgroundRotateAngleProperty, value);
    }

    public static double GetBackgroundRotateAngle(Border element)
    {
        return element.GetValue(BackgroundRotateAngleProperty);
    }
}

/// <summary>
/// TemplatedControl 线性渐变画刷助手，分别支持 Background 和 Foreground
/// </summary>
public class TemplatedControlLinearGradientBrushHelper
{
    public static readonly AttachedProperty<double> BackgroundRotateAngleProperty = AvaloniaProperty.RegisterAttached<
        TemplatedControlLinearGradientBrushHelper,
        TemplatedControl,
        double
    >("BackgroundRotateAngle", coerce: OnBackgroundRotateAngleChanged);

    public static readonly AttachedProperty<double> ForegroundRotateAngleProperty = AvaloniaProperty.RegisterAttached<
        TemplatedControlLinearGradientBrushHelper,
        TemplatedControl,
        double
    >("ForegroundRotateAngle", coerce: OnForegroundRotateAngleChanged);

    private static double OnBackgroundRotateAngleChanged(AvaloniaObject obj, double degrees)
    {
        if (obj is TemplatedControl { Background: LinearGradientBrush brush } control)
        {
            LinearGradientBrushHelperCore.SetGradientRotation(control, brush, degrees);
        }
        return degrees;
    }

    private static double OnForegroundRotateAngleChanged(AvaloniaObject obj, double degrees)
    {
        if (obj is TemplatedControl { Foreground: LinearGradientBrush brush } control)
        {
            LinearGradientBrushHelperCore.SetGradientRotation(control, brush, degrees);
        }
        return degrees;
    }

    public static void SetBackgroundRotateAngle(TemplatedControl element, double value)
    {
        element.SetValue(BackgroundRotateAngleProperty, value);
    }

    public static double GetBackgroundRotateAngle(TemplatedControl element)
    {
        return element.GetValue(BackgroundRotateAngleProperty);
    }

    public static void SetForegroundRotateAngle(TemplatedControl element, double value)
    {
        element.SetValue(ForegroundRotateAngleProperty, value);
    }

    public static double GetForegroundRotateAngle(TemplatedControl element)
    {
        return element.GetValue(ForegroundRotateAngleProperty);
    }
}

internal static class LinearGradientBrushHelperCore
{
    public static void SetGradientRotation(Visual visual, LinearGradientBrush brush, double degrees)
    {
        var rect = new Rect(visual.Bounds.Size);
        SetGradientRotation(rect, brush, degrees);
    }

    public static void SetGradientRotation(Rect rect, LinearGradientBrush brush, double degrees)
    {
        degrees = (degrees % 360 + 360) % 360;
        if (HandleCardinalAngles(brush, degrees))
            return;
        double radians = degrees * Math.PI / 180;
        (double dx, double dy) = (Math.Cos(radians), Math.Sin(radians));
        var start = FindIntersection(rect, -dx, -dy);
        var end = FindIntersection(rect, dx, dy);
        brush.StartPoint = new RelativePoint(start.X / rect.Width, start.Y / rect.Height, RelativeUnit.Relative);
        brush.EndPoint = new RelativePoint(end.X / rect.Width, end.Y / rect.Height, RelativeUnit.Relative);
    }

    private static bool HandleCardinalAngles(LinearGradientBrush brush, double degrees)
    {
        var (start, end) = degrees switch
        {
            0 => (
                new RelativePoint(0.0, 0.5, RelativeUnit.Relative),
                new RelativePoint(1.0, 0.5, RelativeUnit.Relative)
            ),
            90 => (
                new RelativePoint(0.5, 0.0, RelativeUnit.Relative),
                new RelativePoint(0.5, 1.0, RelativeUnit.Relative)
            ),
            180 => (
                new RelativePoint(1.0, 0.5, RelativeUnit.Relative),
                new RelativePoint(0.0, 0.5, RelativeUnit.Relative)
            ),
            270 => (
                new RelativePoint(0.5, 1.0, RelativeUnit.Relative),
                new RelativePoint(0.5, 0.0, RelativeUnit.Relative)
            ),
            _ => default,
        };
        if (start == default)
            return false;

        brush.StartPoint = start;
        brush.EndPoint = end;
        return true;
    }

    private static Point FindIntersection(Rect rect, double dx, double dy)
    {
        var center = rect.Center;
        double nearestT = double.MaxValue;
        Point intersection = default;
        CheckBoundary(0, center.X, dx, rect.Top, t => new Point(0, center.Y + dy * t));
        CheckBoundary(rect.Width, center.X, dx, rect.Top, t => new Point(rect.Width, center.Y + dy * t));
        CheckBoundary(rect.Top, center.Y, dy, rect.Left, t => new Point(center.X + dx * t, 0));
        CheckBoundary(rect.Bottom, center.Y, dy, rect.Left, t => new Point(center.X + dx * t, rect.Height));
        return intersection;
        void CheckBoundary(
            double boundary,
            double centerComponent,
            double direction,
            double min,
            Func<double, Point> pointFactory
        )
        {
            if (direction == 0)
                return;
            double t = (boundary - centerComponent) / direction;
            if (t <= 0)
                return;
            var p = pointFactory(t);

            if (!(p.X >= min) || !(p.X <= rect.Right) || !(p.Y >= min) || !(p.Y <= rect.Bottom) || !(t < nearestT))
                return;
            nearestT = t;
            intersection = p;
        }
    }
}
