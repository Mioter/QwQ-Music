using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;
using Irihi.Avalonia.Shared.Helpers;
using QwQ_Music.Common.Services.Shader;
using SkiaSharp;

namespace QwQ_Music.UI.Controls;

/// <summary>
///     着色器效果控件，用于渲染GLSL着色器
/// </summary>
public class ShaderEffectControl : Control {
    // 添加可绑定的着色器代码属性
    public static readonly StyledProperty<string> ShaderCodeProperty =
        AvaloniaProperty.Register<ShaderEffectControl, string>(nameof(ShaderCode));

    // 添加可绑定的动画状态属性
    public static readonly StyledProperty<bool> IsEnableAnimationProperty =
        AvaloniaProperty.Register<ShaderEffectControl, bool>(nameof(IsEnableAnimation), true);

    // 添加性能模式属性
    public static readonly StyledProperty<ShaderPerformanceMode> PerformanceModeProperty =
        AvaloniaProperty.Register<ShaderEffectControl, ShaderPerformanceMode>(
            nameof(PerformanceMode),
            ShaderPerformanceMode.Balanced);

    // 添加颜色列表属性
    public static readonly StyledProperty<Color[]> ColorsProperty =
        AvaloniaProperty.Register<ShaderEffectControl, Color[]>(nameof(Colors));

    // 添加颜色过渡时长属性
    public static readonly StyledProperty<TimeSpan> ColorTransitionDurationProperty =
        AvaloniaProperty.Register<ShaderEffectControl, TimeSpan>(
            nameof(ColorTransitionDuration),
            TimeSpan.FromMilliseconds(400));

    private DispatcherTimer? _animationTimer;
    private bool _isTimerRunning;
    private Color[] _displayColors;
    private Color[]? _transitionStartColors;
    private DateTime _transitionStartTime;
    private Color[]? _transitionTargetColors;
    private bool _isColorTransitionRunning;
    private Vector2? _mousePosition;

    private ShaderService? _shaderService;

    /// <summary>
    ///     初始化着色器效果控件
    /// </summary>
    public ShaderEffectControl() {
        ClipToBounds = true;
        _displayColors = NormalizeColors(Colors);

        // 启用鼠标输入
        PointerMoved += OnPointerMoved;

        // 监听着色器代码属性变化
        this.GetObservable(ShaderCodeProperty).Subscribe(OnShaderCodeChanged);

        // 监听动画状态属性变化
        this.GetObservable(IsEnableAnimationProperty).Subscribe(OnIsAnimatingChanged);

        // 监听性能模式变化
        this.GetObservable(PerformanceModeProperty).Subscribe(OnPerformanceModeChanged);

        // 监听颜色列表属性变化
        this.GetObservable(ColorsProperty).Subscribe(OnColorsChanged);
    }

    /// <summary>
    ///     着色器代码
    /// </summary>
    public string ShaderCode {
        get => GetValue(ShaderCodeProperty);
        set => SetValue(ShaderCodeProperty, value);
    }

    /// <summary>
    ///     是否启用动画
    /// </summary>
    public bool IsEnableAnimation {
        get => GetValue(IsEnableAnimationProperty);
        set => SetValue(IsEnableAnimationProperty, value);
    }

    /// <summary>
    ///     着色器性能模式
    /// </summary>
    public ShaderPerformanceMode PerformanceMode {
        get => GetValue(PerformanceModeProperty);
        set => SetValue(PerformanceModeProperty, value);
    }

    /// <summary>
    ///     着色器使用的颜色列表
    /// </summary>
    public Color[] Colors {
        get => GetValue(ColorsProperty);
        set => SetValue(ColorsProperty, value);
    }

    /// <summary>
    ///     颜色切换的过渡时长
    /// </summary>
    public TimeSpan ColorTransitionDuration {
        get => GetValue(ColorTransitionDurationProperty);
        set => SetValue(ColorTransitionDurationProperty, value);
    }

    private void OnShaderCodeChanged(string shaderCode) {
        if (string.IsNullOrEmpty(shaderCode))
            return;

        _shaderService = new ShaderService(shaderCode) { Colors = _displayColors };

        InvalidateVisual();
    }

    private void OnColorsChanged(Color[] colors) {
        Color[] targetColors = NormalizeColors(colors);

        if (_displayColors.Length == 0) {
            _displayColors = targetColors;

            _shaderService?.Colors = _displayColors;

            InvalidateVisual();

            return;
        }

        if (_shaderService == null) {
            _displayColors = targetColors;

            return;
        }

        if (AreColorsEqual(_displayColors, targetColors))
            return;

        if (ColorTransitionDuration <= TimeSpan.Zero) {
            _isColorTransitionRunning = false;
            _transitionStartColors = null;
            _transitionTargetColors = null;
            _displayColors = targetColors;
            _shaderService.Colors = _displayColors;
            RefreshTimerInterval();
            InvalidateVisual();

            return;
        }

        _transitionStartColors = _displayColors.ToArray();
        _transitionTargetColors = targetColors;
        _transitionStartTime = DateTime.Now;
        _isColorTransitionRunning = true;

        RefreshTimerInterval();
        EnsureTimerRunning();
        InvalidateVisual();
    }

    private void OnIsAnimatingChanged(bool _) {
        if (ShouldKeepTimerRunning())
            EnsureTimerRunning();
        else
            StopAnimation();
    }

    private void OnPerformanceModeChanged(ShaderPerformanceMode _) {
        RefreshTimerInterval();
    }

    private bool ShouldKeepTimerRunning() { return IsEnableAnimation || _isColorTransitionRunning; }

    private void EnsureTimerRunning() {
        if (!ShouldKeepTimerRunning() || _isTimerRunning)
            return;

        _isTimerRunning = true;

        // 使用DispatcherTimer代替直接递归调用
        if (_animationTimer == null) {
            _animationTimer = new DispatcherTimer { Interval = GetActiveTimerInterval() };

            _animationTimer.Tick += AnimationTimer_Tick;
        }

        RefreshTimerInterval();
        _animationTimer.Start();
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e) {
        _mousePosition = e.GetPosition(this).ToVector2();
        InvalidateVisual();
    }

    private void StartAnimation() {
        EnsureTimerRunning();
    }

    private void StopAnimation() {
        _isTimerRunning = false;
        _animationTimer?.Stop();
    }

    private void AnimationTimer_Tick(object? sender, EventArgs e) {
        if (!ShouldKeepTimerRunning()) {
            StopAnimation();

            return;
        }

        RefreshTimerInterval();
        UpdateFrame();
    }

    private TimeSpan GetTimerInterval() {
        // 根据性能模式设置不同的刷新率
        return PerformanceMode switch {
            ShaderPerformanceMode.HighQuality => TimeSpan.FromMilliseconds(16), // ~60fps
            ShaderPerformanceMode.Balanced    => TimeSpan.FromMilliseconds(33), // ~30fps
            ShaderPerformanceMode.PowerSaver  => TimeSpan.FromMilliseconds(66), // ~15fps
            _                                 => TimeSpan.FromMilliseconds(33)
        };
    }

    private TimeSpan GetActiveTimerInterval() {
        TimeSpan baseInterval = GetTimerInterval();

        // 颜色过渡期间将帧率翻倍（帧间隔减半），过渡结束后恢复基础帧率
        if (_isColorTransitionRunning)
            return TimeSpan.FromMilliseconds(Math.Max(1, baseInterval.TotalMilliseconds / 2.0));

        return baseInterval;
    }

    private void RefreshTimerInterval() {
        if (_animationTimer == null)
            return;

        TimeSpan interval = GetActiveTimerInterval();
        if (_animationTimer.Interval != interval)
            _animationTimer.Interval = interval;
    }

    private void UpdateFrame() {
        DateTime now = DateTime.Now;
        bool shouldInvalidate = false;

        if (_isColorTransitionRunning)
            shouldInvalidate = UpdateColorTransition(now) || shouldInvalidate;

        if (IsEnableAnimation)
            shouldInvalidate = true;

        if (shouldInvalidate)
            InvalidateVisual();
    }

    private bool UpdateColorTransition(DateTime now) {
        if (_transitionStartColors == null || _transitionTargetColors == null || _shaderService == null) {
            _isColorTransitionRunning = false;
            RefreshTimerInterval();

            return false;
        }

        double durationMs = ColorTransitionDuration.TotalMilliseconds;

        if (durationMs <= 0) {
            _displayColors = _transitionTargetColors.ToArray();
            _shaderService.Colors = _displayColors;
            _transitionStartColors = null;
            _transitionTargetColors = null;
            _isColorTransitionRunning = false;
            RefreshTimerInterval();

            return true;
        }

        double progress = Math.Clamp((now - _transitionStartTime).TotalMilliseconds / durationMs, 0, 1);
        double easedProgress = EaseInOut(progress);
        _displayColors = InterpolateColors(_transitionStartColors, _transitionTargetColors, easedProgress);
        _shaderService.Colors = _displayColors;

        if (progress < 1)
            return true;

        _transitionStartColors = null;
        _transitionTargetColors = null;
        _isColorTransitionRunning = false;
        RefreshTimerInterval();

        return true;
    }

    private static Color[] NormalizeColors(Color[]? colors) { return colors?.ToArray() ?? []; }

    private static bool AreColorsEqual(Color[] left, Color[] right) {
        if (left.Length != right.Length)
            return false;

        for (int i = 0; i < left.Length; i++)
            if (left[i] != right[i])
                return false;

        return true;
    }

    private static Color[] InterpolateColors(Color[] from, Color[] to, double progress) {
        int colorCount = Math.Max(from.Length, to.Length);

        if (colorCount == 0)
            return [];

        var result = new Color[colorCount];

        for (int i = 0; i < colorCount; i++) {
            Color start = GetColorAtOrFallback(from, i);
            Color end = GetColorAtOrFallback(to, i);

            double startR = SrgbToLinear(start.R / 255.0);
            double startG = SrgbToLinear(start.G / 255.0);
            double startB = SrgbToLinear(start.B / 255.0);
            double endR = SrgbToLinear(end.R / 255.0);
            double endG = SrgbToLinear(end.G / 255.0);
            double endB = SrgbToLinear(end.B / 255.0);

            byte alpha = LerpByte(start.A, end.A, progress);
            byte red = ToByte(LinearToSrgb(LerpDouble(startR, endR, progress)));
            byte green = ToByte(LinearToSrgb(LerpDouble(startG, endG, progress)));
            byte blue = ToByte(LinearToSrgb(LerpDouble(startB, endB, progress)));

            result[i] = Color.FromArgb(
                alpha,
                red,
                green,
                blue);
        }

        return result;
    }

    private static Color GetColorAtOrFallback(Color[] colors, int index) {
        if (colors.Length == 0)
            return Avalonia.Media.Colors.Transparent;

        if (index < colors.Length)
            return colors[index];

        return colors[^1];
    }

    private static byte LerpByte(byte from, byte to, double progress) {
        return (byte)Math.Clamp((int)Math.Round(from + (to - from) * progress), byte.MinValue, byte.MaxValue);
    }

    private static double LerpDouble(double from, double to, double progress) { return from + (to - from) * progress; }

    private static double EaseInOut(double progress) { return progress * progress * (3.0 - 2.0 * progress); }

    private static double SrgbToLinear(double value) {
        return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
    }

    private static double LinearToSrgb(double value) {
        return value <= 0.0031308 ? value * 12.92 : 1.055 * Math.Pow(value, 1.0 / 2.4) - 0.055;
    }

    private static byte ToByte(double normalized) {
        return (byte)Math.Clamp((int)Math.Round(normalized * 255.0), byte.MinValue, byte.MaxValue);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e) {
        base.OnDetachedFromVisualTree(e);
        StopAnimation();

        // 清理资源
        if (_animationTimer != null) {
            _animationTimer.Tick -= AnimationTimer_Tick;
            _animationTimer = null;
        }

        PointerMoved -= OnPointerMoved;
    }

    public override void Render(DrawingContext context) {
        base.Render(context);

        // 如果没有着色器服务，不进行渲染
        if (_shaderService == null)
            return;

        Size size = Bounds.Size;

        if (size.Width <= 0 || size.Height <= 0)
            return;

        // 使用自定义绘制操作
        var customDrawOp = new ShaderDrawOperation(
            new Rect(0, 0, size.Width, size.Height),
            _shaderService,
            size,
            _mousePosition);

        context.Custom(customDrawOp);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e) {
        base.OnAttachedToVisualTree(e);

        if (ShouldKeepTimerRunning())
            StartAnimation();
    }

    /// <summary>
    ///     自定义绘制操作，用于渲染着色器
    /// </summary>
    private class ShaderDrawOperation(Rect bounds, ShaderService shaderService, Size size, Vector2? mousePosition)
        : ICustomDrawOperation {
        public void Dispose() { }

        public Rect Bounds => bounds;

        public bool HitTest(Point p) { return bounds.Contains(p); }

        public bool Equals(ICustomDrawOperation? other) { return false; }

        public void Render(ImmediateDrawingContext context) {
            // 获取SkiaSharp画布
            var leaseFeature = context.PlatformImpl.GetFeature<ISkiaSharpApiLeaseFeature>();

            if (leaseFeature == null)
                return;

            using ISkiaSharpApiLease lease = leaseFeature.Lease();
            SKCanvas canvas = lease.SkCanvas;

            using SKShader shader = shaderService.CreateShader(size, mousePosition);
            using var paint = new SKPaint();
            paint.Shader = shader;
            paint.IsAntialias = true;

            canvas.DrawRect(new SKRect(0, 0, (float)size.Width, (float)size.Height), paint);
        }
    }
}

/// <summary>
///     点转换扩展方法
/// </summary>
public static class PointExtensions {
    public static Vector2 ToVector2(this Point point) { return new Vector2((float)point.X, (float)point.Y); }
}

/// <summary>
///     着色器性能模式
/// </summary>
public enum ShaderPerformanceMode {
    /// <summary>
    ///     高质量模式 (~60fps)
    /// </summary>
    HighQuality,

    /// <summary>
    ///     平衡模式 (~30fps)
    /// </summary>
    Balanced,

    /// <summary>
    ///     省电模式 (~15fps)
    /// </summary>
    PowerSaver
}