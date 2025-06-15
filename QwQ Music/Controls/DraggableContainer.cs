using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Metadata;

namespace QwQ_Music.Controls;

public class DraggableContainer : TemplatedControl
{
    // 定义附加属性 IsDraggable
    public static readonly AttachedProperty<bool> IsDraggableProperty = AvaloniaProperty.RegisterAttached<
        Control,
        bool
    >("IsDraggable", typeof(DraggableContainer), true);

    public static bool GetIsDraggable(Control control)
    {
        return control.GetValue(IsDraggableProperty);
    }

    public static void SetIsDraggable(Control control, bool value)
    {
        control.SetValue(IsDraggableProperty, value);
    }

    // 允许叠放的属性
    public static readonly StyledProperty<bool> AllowOverlapProperty = AvaloniaProperty.Register<
        DraggableContainer,
        bool
    >(nameof(AllowOverlap), defaultValue: true);

    public bool AllowOverlap
    {
        get => GetValue(AllowOverlapProperty);
        set => SetValue(AllowOverlapProperty, value);
    }

    // 添加角度和距离的可绑定属性
    public static readonly StyledProperty<double> CurrentAngleProperty = AvaloniaProperty.Register<
        DraggableContainer,
        double
    >(nameof(CurrentAngle), defaultBindingMode: BindingMode.OneWayToSource);

    public double CurrentAngle
    {
        get => GetValue(CurrentAngleProperty);
        set => SetValue(CurrentAngleProperty, value);
    }

    public static readonly StyledProperty<double> CurrentDistanceProperty = AvaloniaProperty.Register<
        DraggableContainer,
        double
    >(nameof(CurrentDistance), defaultBindingMode: BindingMode.OneWayToSource);

    public double CurrentDistance
    {
        get => GetValue(CurrentDistanceProperty);
        set => SetValue(CurrentDistanceProperty, value);
    }

    // 内容属性（用于管理子控件）
    [Content]
    public Avalonia.Controls.Controls Children => _children ??= [];

    // 中心点属性
    public Point CenterPoint { get; private set; }

    private Canvas? _innerCanvas;
    private Avalonia.Controls.Controls? _children;
    private Control? _draggedControl;
    private Point _dragStart;
    private Point _controlStartPosition;

    // 添加事件，当子控件位置变化时触发
    public event EventHandler<PositionChangedEventArgs>? PositionChanged;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _innerCanvas = e.NameScope.Find<Canvas>("PART_InnerCanvas");

        // 将内容属性中的子控件添加到内部 Canvas
        if (_innerCanvas == null || _children == null)
            return;

        foreach (var child in _children)
        {
            _innerCanvas.Children.Add(child);
        }

        _innerCanvas.Loaded += PositionChildAtCenter;
        Unloaded += OnUnloaded;
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        Unloaded -= OnUnloaded;
        if (_innerCanvas != null)
            _innerCanvas.Loaded -= PositionChildAtCenter;
    }

    private void PositionChildAtCenter(object? o, RoutedEventArgs e)
    {
        if (_innerCanvas == null || _children == null)
            return;

        // 更新容器中心点
        CenterPoint = new Point(_innerCanvas.Bounds.Width.NaNToZero() / 2, _innerCanvas.Bounds.Height.NaNToZero() / 2);

        foreach (var child in _children)
        {
            // 获取用户是否显式设置了 Left/Top
            bool isLeftSet = child.IsSet(Canvas.LeftProperty);
            bool isTopSet = child.IsSet(Canvas.TopProperty);

            // 如果用户已经设置了位置，跳过自动居中
            if (isLeftSet && isTopSet)
            {
                continue;
            }

            // 获取子控件尺寸
            double childWidth = child.Bounds.Width.NaNToZero();
            double childHeight = child.Bounds.Height.NaNToZero();

            // 如果尺寸为 0，可能布局未完成，跳过
            if (childWidth == 0 || childHeight == 0)
            {
                return;
            }

            // 计算居中位置（仅设置未显式指定的坐标）
            double centerX = isLeftSet ? Canvas.GetLeft(child) : CenterPoint.X - childWidth / 2;
            double centerY = isTopSet ? Canvas.GetTop(child) : CenterPoint.Y - childHeight / 2;

            // 应用坐标
            if (!isLeftSet)
                Canvas.SetLeft(child, centerX);
            if (!isTopSet)
                Canvas.SetTop(child, centerY);
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        var point = e.GetCurrentPoint(this);
        if (
            e.Source is not Control control
            || _innerCanvas?.Children.Contains(control) != true
            || !point.Properties.IsLeftButtonPressed
        )
            return;

        // 检查控件是否可拖动
        if (!GetIsDraggable(control))
        {
            return;
        }

        StartDrag(control, e);
        e.Handled = true;
    }

    private void StartDrag(Control control, PointerPressedEventArgs e)
    {
        _draggedControl = control;
        _dragStart = e.GetPosition(_innerCanvas);
        _controlStartPosition = new Point(Canvas.GetLeft(control).NaNToZero(), Canvas.GetTop(control).NaNToZero());

        /*// 将拖动的控件置顶
        if (_innerCanvas != null && _innerCanvas.Children.Contains(control))
        {
            _innerCanvas.Children.Remove(control);
            _innerCanvas.Children.Add(control);
        }*/

        Cursor = new Cursor(StandardCursorType.Hand);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (_draggedControl == null || _innerCanvas == null)
            return;

        // 检查控件是否可拖动
        if (!GetIsDraggable(_draggedControl))
        {
            return;
        }

        // 获取当前鼠标位置
        var currentPosition = e.GetPosition(_innerCanvas);
        var offset = currentPosition - _dragStart;

        // 计算新坐标
        double newX = _controlStartPosition.X + offset.X;
        double newY = _controlStartPosition.Y + offset.Y;

        // 获取容器实际尺寸（处理 NaN 值）
        double containerWidth = _innerCanvas.Bounds.Width.NaNToZero();
        double containerHeight = _innerCanvas.Bounds.Height.NaNToZero();

        // 获取圆角半径
        double cornerRadiusX = Math.Max(CornerRadius.TopLeft, CornerRadius.TopRight);
        double cornerRadiusY = Math.Max(CornerRadius.BottomLeft, CornerRadius.BottomRight);

        // 限制到容器边界
        (newX, newY) = ClampToBounds(newX, newY, containerWidth, containerHeight, cornerRadiusX, cornerRadiusY);

        // 如果不允许叠放，调整位置避免与其他控件重叠
        if (!AllowOverlap)
        {
            (newX, newY) = AvoidOverlap(newX, newY);
        }

        // 应用新坐标
        Canvas.SetLeft(_draggedControl, newX);
        Canvas.SetTop(_draggedControl, newY);

        // 计算相对中心点的角度和距离
        (double angle, double distance) = CalculateSpatialParameters(currentPosition);

        // 更新可绑定属性
        CurrentAngle = angle;
        CurrentDistance = distance;

        // 触发事件
        PositionChanged?.Invoke(this, new PositionChangedEventArgs(angle, distance));
    }

    private (double X, double Y) ClampToBounds(
        double x,
        double y,
        double width,
        double height,
        double cornerRadiusX,
        double cornerRadiusY
    )
    {
        // 子控件的尺寸
        double childWidth = _draggedControl?.Bounds.Width ?? 0;
        double childHeight = _draggedControl?.Bounds.Height ?? 0;

        // 子控件的边界框
        var childBounds = new Rect(x, y, childWidth, childHeight);

        // 圆角区域的四个角的中心点
        double topRightCenterX = width - cornerRadiusX;
        double bottomLeftCenterY = height - cornerRadiusY;
        double bottomRightCenterX = width - cornerRadiusX;
        double bottomRightCenterY = height - cornerRadiusY;

        // 检查左上角
        if (childBounds.Left < cornerRadiusX && childBounds.Top < cornerRadiusY)
        {
            double distance = CalculateDistance(childBounds.Left, childBounds.Top, cornerRadiusX, cornerRadiusY);
            if (distance > cornerRadiusX)
            {
                double angle = Math.Atan2(childBounds.Top - cornerRadiusY, childBounds.Left - cornerRadiusX);
                x = cornerRadiusX + cornerRadiusX * Math.Cos(angle);
                y = cornerRadiusY + cornerRadiusX * Math.Sin(angle);
            }
        }

        // 检查右上角
        if (childBounds.Right > topRightCenterX && childBounds.Top < cornerRadiusY)
        {
            double distance = CalculateDistance(childBounds.Right, childBounds.Top, topRightCenterX, cornerRadiusY);
            if (distance > cornerRadiusX)
            {
                double angle = Math.Atan2(childBounds.Top - cornerRadiusY, childBounds.Right - topRightCenterX);
                x = topRightCenterX + cornerRadiusX * Math.Cos(angle) - childWidth;
                y = cornerRadiusY + cornerRadiusX * Math.Sin(angle);
            }
        }

        // 检查左下角
        if (childBounds.Left < cornerRadiusX && childBounds.Bottom > bottomLeftCenterY)
        {
            double distance = CalculateDistance(childBounds.Left, childBounds.Bottom, cornerRadiusX, bottomLeftCenterY);
            if (distance > cornerRadiusX)
            {
                double angle = Math.Atan2(childBounds.Bottom - bottomLeftCenterY, childBounds.Left - cornerRadiusX);
                x = cornerRadiusX + cornerRadiusX * Math.Cos(angle);
                y = bottomLeftCenterY + cornerRadiusX * Math.Sin(angle) - childHeight;
            }
        }

        // 检查右下角
        if (childBounds.Right > bottomRightCenterX && childBounds.Bottom > bottomRightCenterY)
        {
            double distance = CalculateDistance(
                childBounds.Right,
                childBounds.Bottom,
                bottomRightCenterX,
                bottomRightCenterY
            );
            if (distance > cornerRadiusX)
            {
                double angle = Math.Atan2(
                    childBounds.Bottom - bottomRightCenterY,
                    childBounds.Right - bottomRightCenterX
                );
                x = bottomRightCenterX + cornerRadiusX * Math.Cos(angle) - childWidth;
                y = bottomRightCenterY + cornerRadiusX * Math.Sin(angle) - childHeight;
            }
        }

        // 确保子控件不会超出容器边界
        x = Math.Clamp(x, 0, width - childWidth);
        y = Math.Clamp(y, 0, height - childHeight);

        return (x, y);
    }

    private static double CalculateDistance(double x1, double y1, double x2, double y2)
    {
        return Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
    }

    // 计算角度和距离
    private (double Angle, double Distance) CalculateSpatialParameters(Point controlPosition)
    {
        // 相对中心点的坐标差
        double dx = controlPosition.X - CenterPoint.X;
        double dy = controlPosition.Y - CenterPoint.Y;

        // 计算距离（保持原有逻辑）
        double maxDistance =
            Math.Sqrt(Math.Pow(_innerCanvas!.Bounds.Width, 2) + Math.Pow(_innerCanvas.Bounds.Height, 2)) / 2;
        double distance = Math.Sqrt(dx * dx + dy * dy) / maxDistance * 100;

        // 关键修正：增加 90 度偏移补偿
        double angleRadians = Math.Atan2(-dy, dx) + Math.PI / 2; // +90 度
        double angleDegrees = angleRadians * (180 / Math.PI);

        // 规范化到 [-180, 180]
        angleDegrees %= 360;
        if (angleDegrees > 180)
            angleDegrees -= 360;
        else if (angleDegrees < -180)
            angleDegrees += 360;

        return (angleDegrees, distance);
    }

    // 避免与其他子控件重叠
    private (double X, double Y) AvoidOverlap(double newX, double newY)
    {
        if (_innerCanvas == null || _draggedControl == null)
            return (newX, newY);

        // 获取拖动控件的边界框、中心点和半径
        var draggedBounds = new Rect(newX, newY, _draggedControl.Bounds.Width, _draggedControl.Bounds.Height);
        var draggedCenter = new Point(draggedBounds.Center.X, draggedBounds.Center.Y);
        double draggedRadius = Math.Max(draggedBounds.Width, draggedBounds.Height) / 2;

        // 获取容器的实际尺寸
        double containerWidth = _innerCanvas.Bounds.Width.NaNToZero();
        double containerHeight = _innerCanvas.Bounds.Height.NaNToZero();

        // 获取圆角半径
        double cornerRadiusX = Math.Max(CornerRadius.TopLeft, CornerRadius.TopRight);
        double cornerRadiusY = Math.Max(CornerRadius.BottomLeft, CornerRadius.BottomRight);

        foreach (var child in _innerCanvas.Children)
        {
            if (child == _draggedControl || child is null)
                continue;

            // 获取当前子控件的边界框、中心点和半径
            double otherLeft = Canvas.GetLeft(child).NaNToZero();
            double otherTop = Canvas.GetTop(child).NaNToZero();
            var otherBounds = new Rect(otherLeft, otherTop, child.Bounds.Width, child.Bounds.Height);
            var otherCenter = new Point(otherBounds.Center.X, otherBounds.Center.Y);
            double otherRadius = Math.Max(otherBounds.Width, otherBounds.Height) / 2;

            // 计算两个圆形之间的距离
            double distance = Distance(draggedCenter, otherCenter);
            double radiusSum = draggedRadius + otherRadius;

            // 如果发生碰撞
            if (distance < radiusSum)
            {
                // 计算碰撞方向向量
                double dx = draggedCenter.X - otherCenter.X;
                double dy = draggedCenter.Y - otherCenter.Y;

                // 归一化方向向量
                double length = Math.Sqrt(dx * dx + dy * dy);
                dx /= length;
                dy /= length;

                // 调整拖动控件的位置，使其沿阻挡物的边缘滑动
                double overlap = radiusSum - distance;
                newX += dx * overlap;
                newY += dy * overlap;

                // 更新拖动控件的中心点
                draggedCenter = new Point(newX + draggedBounds.Width / 2, newY + draggedBounds.Height / 2);

                // 确保调整后不会超出容器边界
                (newX, newY) = ClampToBounds(newX, newY, containerWidth, containerHeight, cornerRadiusX, cornerRadiusY);
            }
        }

        return (newX, newY);
    }

    private static double Distance(Point p1, Point p2)
    {
        return Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _draggedControl = null;
        Cursor = Cursor.Default;
    }
}

public static class DoubleExtensions
{
    public static double NaNToZero(this double value) => double.IsNaN(value) ? 0 : value;
}

// 事件参数类
public class PositionChangedEventArgs(double angle, double distance) : EventArgs
{
    public double Angle { get; } = angle;
    public double Distance { get; } = distance;
}
