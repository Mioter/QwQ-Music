using System;
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
using SkiaSharp;

namespace QwQ_Music.Controls;

/// <summary>
/// 着色器效果控件，用于渲染GLSL着色器
/// </summary>
public class ShaderEffectControl : Control
{
    private readonly ShaderService _shaderService;
    private Vector2? _mousePosition;
    private bool _animating = true;
    private DateTime _lastRenderTime = DateTime.Now;

    /// <summary>
    /// 初始化着色器效果控件
    /// </summary>
    /// <param name="shaderCode">GLSL着色器代码</param>
    public ShaderEffectControl(string shaderCode)
    {
        _shaderService = new ShaderService(shaderCode);
        ClipToBounds = true;
            
        // 启用鼠标输入
        PointerMoved += OnPointerMoved;
        PointerPressed += OnPointerPressed;
        PointerReleased += OnPointerReleased;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        _mousePosition = e.GetPosition(this).ToVector2();
        InvalidateVisual();
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _mousePosition = e.GetPosition(this).ToVector2();
        InvalidateVisual();
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _mousePosition = null;
        InvalidateVisual();
    }

    /// <summary>
    /// 是否启用动画
    /// </summary>
    public new bool IsAnimating
    {
        get => _animating;
        set
        {
            _animating = value;
            if (_animating)
            {
                StartAnimation();
            }
        }
    }

    private void StartAnimation()
    {
        if (!_animating) return;
            
        var now = DateTime.Now;
        double elapsed = (now - _lastRenderTime).TotalMilliseconds;
            
        // 限制帧率，避免过度渲染
        if (elapsed > 16) // ~60fps
        {
            _lastRenderTime = now;
            InvalidateVisual();
        }
            
        // 请求下一帧
        Dispatcher.UIThread.Post(StartAnimation, DispatcherPriority.Render);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var size = Bounds.Size;
        if (size.Width <= 0 || size.Height <= 0) return;

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
        if (_animating)
        {
            StartAnimation();
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _animating = false;
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

        // 修复Render方法，使用新的API获取SkiaSharp画布
        public void Render(ImmediateDrawingContext context)
        {
            // 使用新的API获取SkiaSharp画布
            var leaseFeature = context.PlatformImpl.GetFeature<ISkiaSharpApiLeaseFeature>();
            if (leaseFeature == null)
                return;

            using var lease = leaseFeature.Lease();
            var canvas = lease.SkCanvas;
                
            using var shader = shaderService.CreateShader(size, mousePosition);
            using var paint = new SKPaint
            {
                Shader = shader,
                IsAntialias = true,
            };

            canvas.DrawRect(
                new SKRect(0, 0, (float)size.Width, (float)size.Height),
                paint
            );
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