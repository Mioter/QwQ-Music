using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace QwQ_Music.Helper;

/// <summary>
/// 线性渐变画刷助手，支持以度数设置渐变角度
/// </summary>
public class LinearGradientBrushHelper
{
    /// <summary>
    /// 附加属性：旋转角度（0-360度）
    /// </summary>
    public static readonly AttachedProperty<double> RotateAngleProperty = AvaloniaProperty.RegisterAttached<
        LinearGradientBrushHelper,
        StyledElement,
        double
    >("RotateAngle", coerce: OnRotateAngleChanged);

    private static double OnRotateAngleChanged(AvaloniaObject @object, double degrees)
    {
        if (@object is Border { BorderBrush: LinearGradientBrush brush } border)
        {
            SetGradientRotation(border, brush, degrees);
        }
        return degrees;
    }

    public static void SetRotateAngle(StyledElement element, double value) =>
        element.SetValue(RotateAngleProperty, value);

    public static double GetRotateAngle(StyledElement element) => element.GetValue(RotateAngleProperty);

    /// <summary>
    /// 设置渐变旋转角度（可视化元素版本）
    /// </summary>
    public static void SetGradientRotation(Visual visual, LinearGradientBrush brush, double degrees)
    {
        var rect = new Rect(visual.Bounds.Size);
        SetGradientRotation(rect, brush, degrees);
    }

    /// <summary>
    /// 设置渐变旋转角度（矩形版本）
    /// </summary>
    public static void SetGradientRotation(Rect rect, LinearGradientBrush brush, double degrees)
    {
        // 归一化角度到[0, 360)范围
        degrees = (degrees % 360 + 360) % 360;

        // 处理特殊角度
        if (HandleCardinalAngles(brush, degrees))
            return;

        // 转换为弧度并计算方向向量
        double radians = degrees * Math.PI / 180;
        (double dx, double dy) = (Math.Cos(radians), Math.Sin(radians));

        // 计算起点和终点
        var start = FindIntersection(rect, -dx, -dy); // 反向方向
        var end = FindIntersection(rect, dx, dy); // 正向方向

        // 设置相对坐标
        brush.StartPoint = new RelativePoint(start.X / rect.Width, start.Y / rect.Height, RelativeUnit.Relative);
        brush.EndPoint = new RelativePoint(end.X / rect.Width, end.Y / rect.Height, RelativeUnit.Relative);
    }

    /// <summary>
    /// 处理基本方位角度（0°, 90°, 180°, 270°）
    /// </summary>
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

        if (start != default)
        {
            brush.StartPoint = start;
            brush.EndPoint = end;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 计算射线与矩形边界的交点
    /// </summary>
    private static Point FindIntersection(Rect rect, double dx, double dy)
    {
        var center = rect.Center;
        double nearestT = double.MaxValue;
        Point intersection = default;

        // 检查各边界
        CheckBoundary(0, center.X, dx, rect.Top, t => new Point(0, center.Y + dy * t)); // 左边界
        CheckBoundary(rect.Width, center.X, dx, rect.Top, t => new Point(rect.Width, center.Y + dy * t)); // 右边界
        CheckBoundary(rect.Top, center.Y, dy, rect.Left, t => new Point(center.X + dx * t, 0)); // 上边界
        CheckBoundary(rect.Bottom, center.Y, dy, rect.Left, t => new Point(center.X + dx * t, rect.Height)); // 下边界

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
                return; // 防止除以零

            double t = (boundary - centerComponent) / direction;
            if (t <= 0)
                return;

            var p = pointFactory(t);
            if (p.X >= min && p.X <= rect.Right && p.Y >= min && p.Y <= rect.Bottom && t < nearestT)
            {
                nearestT = t;
                intersection = p;
            }
        }
    }
}
