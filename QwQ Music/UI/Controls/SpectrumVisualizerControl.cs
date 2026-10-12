using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace QwQ_Music.UI.Controls;

/// <summary>
///     频谱可视化控件，以波浪形式展示音频频谱数据
/// </summary>
public class SpectrumVisualizerControl : Control {
    static SpectrumVisualizerControl() {
        AffectsRender<SpectrumVisualizerControl>(LineBrushProperty, LineThicknessProperty, AmplitudeScaleProperty);
    }

    public SpectrumVisualizerControl() {
        ClipToBounds = true;

        // 初始化动画定时器
        _animationTimer = new DispatcherTimer {
            Interval = TimeSpan.FromMilliseconds(32) // ~30fps
        };

        _animationTimer.Tick += OnAnimationTick;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e) {
        base.OnAttachedToVisualTree(e);
        _animationTimer.Start();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e) {
        base.OnDetachedFromVisualTree(e);
        _animationTimer.Stop();
    }

    /// <summary>
    ///     动画定时器事件处理
    /// </summary>
    private void OnAnimationTick(object? sender, EventArgs e) {
        bool needsUpdate = false;

        // 平滑插值当前值到目标值
        for (int i = 0; i < _currentValues.Length && i < _targetValues.Length; i++) {
            float target = _targetValues[i];
            float current = _currentValues[i];
            float diff = target - current;

            // 如果差值很小，直接设置为目标值
            if (Math.Abs(diff) < 0.01) {
                if (!(Math.Abs(_currentValues[i] - target) > 0.001))
                    continue;

                _currentValues[i] = target;
            } else {
                // 使用平滑因子进行插值
                _currentValues[i] += diff * SmoothingFactor;
            }

            needsUpdate = true;
        }

        if (needsUpdate)
            InvalidateVisual();
    }

    public override void Render(DrawingContext context) {
        base.Render(context);

        Rect bounds = Bounds;

        if (bounds is { Width: <= 0 } or { Height: <= 0 })
            return;

        if (_currentValues.Length == 0) {
            // 绘制水平基线
            DrawBaseline(context, bounds);

            return;
        }

        // 使用Catmull-Rom样条曲线绘制频谱波形
        DrawSpectrumWaveSmooth(context, bounds);
    }

    /// <summary>
    ///     绘制基线（当没有数据时）- 修复边界问题
    /// </summary>
    private void DrawBaseline(DrawingContext context, Rect bounds) {
        // 考虑线条粗细，确保基线完全在可视区域内
        double baselineY = bounds.Height - LineThickness;

        Pen pen = new(LineBrush, LineThickness) { LineCap = PenLineCap.Round };

        context.DrawLine(pen, new Point(0, baselineY), new Point(bounds.Width, baselineY));
    }

    /// <summary>
    ///     使用Catmull-Rom样条曲线绘制频谱波形 - 考虑线条粗细的边界处理
    /// </summary>
    private void DrawSpectrumWaveSmooth(DrawingContext context, Rect bounds) {
        double width = bounds.Width;
        double height = bounds.Height - LineThickness; // 考虑线条粗细的有效高度
        int dataCount = _currentValues.Length;

        // 计算每个数据点的X坐标间距
        double stepX = width / (dataCount - 1);

        // 生成数据点
        var points = new Point[dataCount];
        for (int i = 0; i < dataCount; i++) {
            double x = i * stepX;
            double amplitude = float.Log10(_currentValues[i]) * AmplitudeScale;
            double y = height - amplitude * 40;

            // 限制Y坐标在边界内，考虑线条粗细
            y = Math.Clamp(y, LineThickness, height);
            points[i] = new Point(x, y);
        }

        // 创建路径几何
        var geometry = new PathGeometry();

        var figure = new PathFigure { StartPoint = points[0], IsClosed = false };

        if (figure.Segments == null)
            return;

        // 使用Catmull-Rom样条曲线插值
        const int segmentsPerPoint = 8; // 每两个点之间的细分段数

        for (int i = 0; i < dataCount - 1; i++) {
            // 获取插值所需的四个控制点
            Point p0 = i > 0 ? points[i - 1] : points[i];
            Point p1 = points[i];
            Point p2 = points[i + 1];
            Point p3 = i < dataCount - 2 ? points[i + 2] : points[i + 1];

            // 对当前段进行细分插值
            for (int j = 1; j <= segmentsPerPoint; j++) {
                double t = j / (double)segmentsPerPoint;
                Point interpolatedPoint = CatmullRomInterpolate(p0, p1, p2, p3, t);

                // 确保插值点不会超出边界
                interpolatedPoint = new Point(
                    interpolatedPoint.X,
                    Math.Clamp(interpolatedPoint.Y, LineThickness, height));

                figure.Segments.Add(new LineSegment { Point = interpolatedPoint });
            }
        }

        geometry.Figures?.Add(figure);

        // 绘制路径
        var pen = new Pen(LineBrush, LineThickness) { LineCap = PenLineCap.Round, LineJoin = PenLineJoin.Round };

        context.DrawGeometry(null, pen, geometry);
    }

    /// <summary>
    ///     Catmull-Rom样条插值
    /// </summary>
    private static Point CatmullRomInterpolate(Point p0, Point p1, Point p2, Point p3, double t) {
        double t2 = t * t;
        double t3 = t2 * t;

        // Catmull-Rom样条公式
        double x = 0.5 *
                   (2 * p1.X +
                    (-p0.X + p2.X) * t +
                    (2 * p0.X - 5 * p1.X + 4 * p2.X - p3.X) * t2 +
                    (-p0.X + 3 * p1.X - 3 * p2.X + p3.X) * t3);

        double y = 0.5 *
                   (2 * p1.Y +
                    (-p0.Y + p2.Y) * t +
                    (2 * p0.Y - 5 * p1.Y + 4 * p2.Y - p3.Y) * t2 +
                    (-p0.Y + 3 * p1.Y - 3 * p2.Y + p3.Y) * t3);

        return new Point(x, y);
    }

    /*
    /// <summary>
    ///     备选方案：使用三次贝塞尔曲线的平滑波形绘制
    /// </summary>
    private void DrawSpectrumWave(DrawingContext context, Rect bounds)
    {
        if (_currentValues == null || _currentValues.Length == 0)
            return;

        double halfThickness = LineThickness / 2;
        double bottomY = bounds.Height - halfThickness;
        double width = bounds.Width;
        double height = bounds.Height - LineThickness;
        int dataCount = _currentValues.Length;

        double stepX = width / (dataCount - 1);

        // 创建路径几何
        var geometry = new PathGeometry();
        var figure = new PathFigure
        {
            IsClosed = false
        };

        if (figure.Segments == null)
            return;

        // 生成数据点
        var points = new Point[dataCount];
        for (int i = 0; i < dataCount; i++)
        {
            double x = i * stepX;
            double amplitude = _currentValues[i] * AmplitudeScale;
            double y = bottomY - amplitude * height;
            y = Math.Clamp(y, halfThickness, bounds.Height - halfThickness);
            points[i] = new Point(x, y);
        }

        // 设置起始点
        figure.StartPoint = points[0];

        // 使用三次贝塞尔曲线连接所有点
        for (int i = 1; i < dataCount; i++)
        {
            Point previousPoint = points[i - 1];
            Point currentPoint = points[i];

            if (i == 1 || i == dataCount - 1)
            {
                double controlX1 = previousPoint.X + (currentPoint.X - previousPoint.X) * 0.5;
                double controlX2 = previousPoint.X + (currentPoint.X - previousPoint.X) * 0.5;

                figure.Segments.Add(new BezierSegment
                {
                    Point1 = new Point(controlX1, previousPoint.Y),
                    Point2 = new Point(controlX2, currentPoint.Y),
                    Point3 = currentPoint
                });
            }
            else
            {
                Point nextPoint = points[i + 1];
                Point prevPrevPoint = points[i - 2];

                double tangent1X = (currentPoint.X - prevPrevPoint.X) * 0.2;
                double tangent2X = (nextPoint.X - previousPoint.X) * 0.2;

                figure.Segments.Add(new BezierSegment
                {
                    Point1 = new Point(previousPoint.X + tangent1X, previousPoint.Y),
                    Point2 = new Point(currentPoint.X - tangent2X, currentPoint.Y),
                    Point3 = currentPoint
                });
            }
        }

        geometry.Figures?.Add(figure);

        var pen = new Pen(LineBrush, LineThickness)
        {
            LineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Round,
        };

        context.DrawGeometry(null, pen, geometry);
    }
    */

    /// <summary>
    ///     更新频谱数据
    /// </summary>
    public void UpdateSpectrumData(float[] newData) {
        // 初始化数组
        if (_targetValues.Length != newData.Length) {
            _targetValues = new float[newData.Length];
            _currentValues = new float[newData.Length];
        }

        _targetValues = newData;

        InvalidateVisual();
    }

    #region 依赖属性

    /// <summary>
    ///     线条画笔
    /// </summary>
    public static readonly StyledProperty<IBrush> LineBrushProperty =
        AvaloniaProperty.Register<SpectrumVisualizerControl, IBrush>(nameof(LineBrush), Brushes.White);

    /// <summary>
    ///     线条粗细
    /// </summary>
    public static readonly StyledProperty<double> LineThicknessProperty =
        AvaloniaProperty.Register<SpectrumVisualizerControl, double>(nameof(LineThickness), 2.0);

    /// <summary>
    ///     振幅缩放因子（控制波形高度）
    /// </summary>
    public static readonly StyledProperty<double> AmplitudeScaleProperty =
        AvaloniaProperty.Register<SpectrumVisualizerControl, double>(nameof(AmplitudeScale), 0.5);

    /// <summary>
    ///     平滑因子（0-1，值越大变化越快）
    /// </summary>
    public static readonly StyledProperty<float> SmoothingFactorProperty =
        AvaloniaProperty.Register<SpectrumVisualizerControl, float>(nameof(SmoothingFactor), 0.15f);

    #endregion

    #region 属性

    /// <summary>
    ///     线条画笔
    /// </summary>
    public IBrush LineBrush {
        get => GetValue(LineBrushProperty);
        set => SetValue(LineBrushProperty, value);
    }

    /// <summary>
    ///     线条粗细
    /// </summary>
    public double LineThickness {
        get => GetValue(LineThicknessProperty);
        set => SetValue(LineThicknessProperty, value);
    }

    /// <summary>
    ///     振幅缩放因子（控制波形高度）
    /// </summary>
    public double AmplitudeScale {
        get => GetValue(AmplitudeScaleProperty);
        set => SetValue(AmplitudeScaleProperty, value);
    }

    /// <summary>
    ///     平滑因子（0-1，值越大变化越快）
    /// </summary>
    public float SmoothingFactor {
        get => GetValue(SmoothingFactorProperty);
        set => SetValue(SmoothingFactorProperty, value);
    }

    #endregion

    #region 私有字段

    private float[] _targetValues = [];               // 目标值（从频谱数据计算）
    private float[] _currentValues = [];              // 当前显示值（用于动画插值）
    private readonly DispatcherTimer _animationTimer; // 动画定时器

    #endregion
}