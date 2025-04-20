using System;
using System.Collections.Generic;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;
using QwQ_Music.Services.Shader;
using QwQ.Avalonia.Helper;
using SkiaSharp;

namespace QwQ_Music.Controls;

/// <summary>
/// 着色器效果控件，用于渲染GLSL着色器
/// </summary>
public class ShaderEffectControl : Control
{
    // 添加可绑定的着色器代码属性
    public static readonly StyledProperty<string> ShaderCodeProperty = AvaloniaProperty.Register<
        ShaderEffectControl,
        string
    >(nameof(ShaderCode));

    // 添加可绑定的动画状态属性
    public static readonly StyledProperty<bool> IsEnableAnimationProperty = AvaloniaProperty.Register<
        ShaderEffectControl,
        bool
    >(nameof(IsEnableAnimation), true);

    // 添加性能模式属性
    public static readonly StyledProperty<ShaderPerformanceMode> PerformanceModeProperty = AvaloniaProperty.Register<
        ShaderEffectControl,
        ShaderPerformanceMode
    >(nameof(PerformanceMode), ShaderPerformanceMode.Balanced);

    // 添加颜色列表属性
    public static readonly StyledProperty<List<Color>> ColorsProperty = AvaloniaProperty.Register<
        ShaderEffectControl,
        List<Color>
    >(nameof(Colors), [Avalonia.Media.Colors.Blue, Avalonia.Media.Colors.Purple]);

    /// <summary>
    /// 着色器代码
    /// </summary>
    public string ShaderCode
    {
        get => GetValue(ShaderCodeProperty);
        set => SetValue(ShaderCodeProperty, value);
    }

    /// <summary>
    /// 是否启用动画
    /// </summary>
    public bool IsEnableAnimation
    {
        get => GetValue(IsEnableAnimationProperty);
        set => SetValue(IsEnableAnimationProperty, value);
    }

    /// <summary>
    /// 着色器性能模式
    /// </summary>
    public ShaderPerformanceMode PerformanceMode
    {
        get => GetValue(PerformanceModeProperty);
        set => SetValue(PerformanceModeProperty, value);
    }

    /// <summary>
    /// 着色器使用的颜色列表
    /// </summary>
    public List<Color> Colors
    {
        get => GetValue(ColorsProperty);
        set => SetValue(ColorsProperty, value);
    }

    private ShaderService? _shaderService;
    private Vector2? _mousePosition;
    private DateTime _lastRenderTime = DateTime.Now;
    private bool _isAnimationRunning;
    private DispatcherTimer? _animationTimer;

    /// <summary>
    /// 初始化着色器效果控件
    /// </summary>
    public ShaderEffectControl()
    {
        ClipToBounds = true;

        // 启用鼠标输入
        PointerMoved += OnPointerMoved;

        // 监听着色器代码属性变化
        this.GetObservable(ShaderCodeProperty).Subscribe(OnShaderCodeChanged);

        // 监听动画状态属性变化
        this.GetObservable(IsEnableAnimationProperty).Subscribe(OnIsAnimatingChanged);

        // 监听颜色列表属性变化
        this.GetObservable(ColorsProperty).Subscribe(OnColorsChanged);
    }

    private void OnShaderCodeChanged(string shaderCode)
    {
        if (string.IsNullOrEmpty(shaderCode))
            return;

        _shaderService = new ShaderService(shaderCode) { Colors = Colors };
        InvalidateVisual();
    }

    private void OnColorsChanged(List<Color> colors)
    {
        if (_shaderService == null)
            return;

        _shaderService.Colors = colors;
        InvalidateVisual();
    }

    private void OnIsAnimatingChanged(bool isAnimating)
    {
        switch (isAnimating)
        {
            case true when !_isAnimationRunning:
                StartAnimation();
                break;
            case false when _isAnimationRunning:
                StopAnimation();
                break;
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        _mousePosition = e.GetPosition(this).ToVector2();
        InvalidateVisual();
    }

    private void StartAnimation()
    {
        if (!IsEnableAnimation || _isAnimationRunning)
            return;

        _isAnimationRunning = true;

        // 使用DispatcherTimer代替直接递归调用
        if (_animationTimer == null)
        {
            _animationTimer = new DispatcherTimer { Interval = GetTimerInterval() };
            _animationTimer.Tick += AnimationTimer_Tick;
        }

        _animationTimer.Start();
    }

    private void StopAnimation()
    {
        _isAnimationRunning = false;
        _animationTimer?.Stop();
    }

    private void AnimationTimer_Tick(object? sender, EventArgs e)
    {
        if (!_isAnimationRunning)
        {
            _animationTimer?.Stop();
            return;
        }

        UpdateFrame();
    }

    private TimeSpan GetTimerInterval()
    {
        // 根据性能模式设置不同的刷新率
        return PerformanceMode switch
        {
            ShaderPerformanceMode.HighQuality => TimeSpan.FromMilliseconds(16), // ~60fps
            ShaderPerformanceMode.Balanced => TimeSpan.FromMilliseconds(33), // ~30fps
            ShaderPerformanceMode.PowerSaver => TimeSpan.FromMilliseconds(66), // ~15fps
            _ => TimeSpan.FromMilliseconds(33),
        };
    }

    private void UpdateFrame()
    {
        if (!IsEnableAnimation)
        {
            _isAnimationRunning = false;
            return;
        }

        var now = DateTime.Now;
        double elapsed = (now - _lastRenderTime).TotalMilliseconds;

        // 根据性能模式限制帧率
        double frameInterval = PerformanceMode switch
        {
            ShaderPerformanceMode.HighQuality => 16, // ~60fps
            ShaderPerformanceMode.Balanced => 33, // ~30fps
            ShaderPerformanceMode.PowerSaver => 66, // ~15fps
            _ => 33,
        };

        // 限制帧率，避免过度渲染
        if (!(elapsed > frameInterval))
            return;

        _lastRenderTime = now;
        InvalidateVisual();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        StopAnimation();

        // 清理资源
        if (_animationTimer != null)
        {
            _animationTimer.Tick -= AnimationTimer_Tick;
            _animationTimer = null;
        }

        PointerMoved -= OnPointerMoved;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        // 如果没有着色器服务，不进行渲染
        if (_shaderService == null)
            return;

        var size = Bounds.Size;
        if (size.Width <= 0 || size.Height <= 0)
            return;

        // 使用自定义绘制操作
        var customDrawOp = new ShaderDrawOperation(
            new Rect(0, 0, size.Width, size.Height),
            _shaderService,
            size,
            _mousePosition
        );

        context.Custom(customDrawOp);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (IsEnableAnimation)
        {
            StartAnimation();
        }
    }

    /// <summary>
    /// 自定义绘制操作，用于渲染着色器
    /// </summary>
    private class ShaderDrawOperation(Rect bounds, ShaderService shaderService, Size size, Vector2? mousePosition)
        : ICustomDrawOperation
    {
        public void Dispose() { }

        public Rect Bounds => bounds;

        public bool HitTest(Point p) => bounds.Contains(p);

        public bool Equals(ICustomDrawOperation? other) => false;

        public void Render(ImmediateDrawingContext context)
        {
            // 获取SkiaSharp画布
            var leaseFeature = context.PlatformImpl.GetFeature<ISkiaSharpApiLeaseFeature>();
            if (leaseFeature == null)
                return;

            using var lease = leaseFeature.Lease();
            var canvas = lease.SkCanvas;

            using var shader = shaderService.CreateShader(size, mousePosition);
            using var paint = new SKPaint();
            paint.Shader = shader;
            paint.IsAntialias = true;

            canvas.DrawRect(new SKRect(0, 0, (float)size.Width, (float)size.Height), paint);
        }
    }
}

/// <summary>
/// 点转换扩展方法
/// </summary>
public static class PointExtensions
{
    public static Vector2 ToVector2(this Point point)
    {
        return new Vector2((float)point.X, (float)point.Y);
    }
}

/// <summary>
/// 着色器性能模式
/// </summary>
public enum ShaderPerformanceMode
{
    /// <summary>
    /// 高质量模式 (~60fps)
    /// </summary>
    HighQuality,

    /// <summary>
    /// 平衡模式 (~30fps)
    /// </summary>
    Balanced,

    /// <summary>
    /// 省电模式 (~15fps)
    /// </summary>
    PowerSaver,
}
