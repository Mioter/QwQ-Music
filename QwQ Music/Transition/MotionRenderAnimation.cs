using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using QwQ_Music.Common.Services;

namespace QwQ_Music.Transition;

/// <summary>
/// 渲染动画接口，定义动画基本属性
/// </summary>
public interface IRenderAnimation {
    /// <summary>
    /// 动画延迟时间（非中断场景）
    /// </summary>
    TimeSpan Delay { get; }

    /// <summary>
    /// 动画持续时间
    /// </summary>
    TimeSpan Duration { get; }

    /// <summary>
    /// 缓动函数
    /// </summary>
    Easing Easing { get; }

    /// <summary>
    /// 动画播放期间被新动画中断时使用的延迟时间
    /// </summary>
    TimeSpan InterruptDelay { get; }
}

/// <summary>
/// 偏移渲染动画（监听Bounds位置变化，平滑移动视觉位置）
/// </summary>
public class OffsetRenderAnimation : AvaloniaObject, IRenderAnimation {
    public static readonly StyledProperty<TimeSpan> DelayProperty =
        AvaloniaProperty.Register<OffsetRenderAnimation, TimeSpan>(nameof(Delay), TimeSpan.FromMilliseconds(200));

    public static readonly StyledProperty<TimeSpan> DurationProperty =
        AvaloniaProperty.Register<OffsetRenderAnimation, TimeSpan>(nameof(Duration), TimeSpan.FromMilliseconds(400));

    public static readonly StyledProperty<Easing> EasingProperty =
        AvaloniaProperty.Register<OffsetRenderAnimation, Easing>(nameof(Easing), new QuadraticEaseOut());

    public static readonly StyledProperty<TimeSpan> InterruptDelayProperty =
        AvaloniaProperty.Register<OffsetRenderAnimation, TimeSpan>(
            nameof(InterruptDelay),
            TimeSpan.FromMilliseconds(20));

    public TimeSpan Delay {
        get => GetValue(DelayProperty);
        set => SetValue(DelayProperty, value);
    }

    public TimeSpan Duration {
        get => GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }

    public Easing Easing {
        get => GetValue(EasingProperty);
        set => SetValue(EasingProperty, value);
    }

    public TimeSpan InterruptDelay {
        get => GetValue(InterruptDelayProperty);
        set => SetValue(InterruptDelayProperty, value);
    }
}

/// <summary>
/// 尺寸渲染动画（监听Bounds尺寸变化，平滑缩放视觉效果）
/// </summary>
public class SizeRenderAnimation : AvaloniaObject, IRenderAnimation {
    public static readonly StyledProperty<TimeSpan> DelayProperty =
        AvaloniaProperty.Register<SizeRenderAnimation, TimeSpan>(nameof(Delay), TimeSpan.FromMilliseconds(200));

    public static readonly StyledProperty<TimeSpan> DurationProperty =
        AvaloniaProperty.Register<SizeRenderAnimation, TimeSpan>(nameof(Duration), TimeSpan.FromMilliseconds(400));

    public static readonly StyledProperty<Easing> EasingProperty =
        AvaloniaProperty.Register<SizeRenderAnimation, Easing>(nameof(Easing), new QuadraticEaseOut());

    public static readonly StyledProperty<TimeSpan> InterruptDelayProperty =
        AvaloniaProperty.Register<SizeRenderAnimation, TimeSpan>(nameof(InterruptDelay), TimeSpan.FromMilliseconds(20));

    public TimeSpan Delay {
        get => GetValue(DelayProperty);
        set => SetValue(DelayProperty, value);
    }

    public TimeSpan Duration {
        get => GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }

    public Easing Easing {
        get => GetValue(EasingProperty);
        set => SetValue(EasingProperty, value);
    }

    public TimeSpan InterruptDelay {
        get => GetValue(InterruptDelayProperty);
        set => SetValue(InterruptDelayProperty, value);
    }
}

/// <summary>
/// 动画集合，用于XAML中定义多个渲染动画
/// </summary>
public class RenderAnimations : List<IRenderAnimation> { }

internal class AnimationRunner(Visual target, IRenderAnimation animation) : IDisposable {
    private CancellationTokenSource? _delayCancellation;
    private bool _isAnimatingOrDelayed;

    public void StartSizeAnimation(Size fromScale, Size toScale) {
        if (Math.Abs(fromScale.Width - toScale.Width) < 0.0001 && Math.Abs(fromScale.Height - toScale.Height) < 0.0001)
            return;

        bool wasActive = _isAnimatingOrDelayed;

        // 当动画被中断时，读取当前渲染缩放值（即 ScaleTransform 的实时值），
        // 而非基于布局尺寸重新计算起始缩放
        Size actualFromScale = wasActive ?
            new Size(target.GetValue(ScaleTransform.ScaleXProperty), target.GetValue(ScaleTransform.ScaleYProperty)) :
            fromScale;

        if (Math.Abs(actualFromScale.Width - toScale.Width) < 0.0001 &&
            Math.Abs(actualFromScale.Height - toScale.Height) < 0.0001)
            return;

        CancelCurrent();

        TimeSpan actualDelay = wasActive ? animation.InterruptDelay : animation.Delay;

        var animation1 = new Animation {
            Delay = actualDelay,
            Duration = animation.Duration,
            Easing = animation.Easing,
            FillMode = FillMode.Backward,
            Children = {
                new KeyFrame {
                    Cue = new Cue(0d),
                    Setters = {
                        new Setter(ScaleTransform.ScaleXProperty, actualFromScale.Width),
                        new Setter(ScaleTransform.ScaleYProperty, actualFromScale.Height)
                    }
                },
                new KeyFrame {
                    Cue = new Cue(1d),
                    Setters = {
                        new Setter(ScaleTransform.ScaleXProperty, toScale.Width),
                        new Setter(ScaleTransform.ScaleYProperty, toScale.Height)
                    }
                }
            }
        };

        _isAnimatingOrDelayed = true;
        _delayCancellation = new CancellationTokenSource();

        Dispatcher.UIThread.Post(
            async void () => {
                try {
                    if (_delayCancellation.IsCancellationRequested)
                        return;
                    try {
                        await animation1.RunAsync(target, _delayCancellation.Token).ConfigureAwait(true);
                    } catch (OperationCanceledException) { } finally {
                        if (!_delayCancellation.IsCancellationRequested)
                            _isAnimatingOrDelayed = false;
                    }
                } catch (Exception ex) {
                    await LoggerService.ErrorAsync("SizeAnimation异常", ex).ConfigureAwait(true);
                }
            },
            DispatcherPriority.Render);
    }

    public void StartOffsetAnimation(Vector layoutDelta) {
        if (layoutDelta.X == 0 && layoutDelta.Y == 0)
            return;

        bool wasActive = _isAnimatingOrDelayed;

        // Recompute based on the current render offset, then apply immediately so
        // parent layout changes do not cause a visible jump before the animation starts.
        double currentX = target.GetValue(TranslateTransform.XProperty);
        double currentY = target.GetValue(TranslateTransform.YProperty);
        Point actualFromOffset = new(currentX + layoutDelta.X, currentY + layoutDelta.Y);

        if (actualFromOffset == default)
            return;

        CancelCurrent();

        target.SetValue(TranslateTransform.XProperty, actualFromOffset.X);
        target.SetValue(TranslateTransform.YProperty, actualFromOffset.Y);

        TimeSpan actualDelay = wasActive ? animation.InterruptDelay : animation.Delay;

        var animation1 = new Animation {
            Delay = actualDelay,
            Duration = animation.Duration,
            Easing = animation.Easing,
            FillMode = FillMode.Backward,
            Children = {
                new KeyFrame {
                    Cue = new Cue(0d),
                    Setters = {
                        new Setter(TranslateTransform.XProperty, actualFromOffset.X),
                        new Setter(TranslateTransform.YProperty, actualFromOffset.Y)
                    }
                },
                new KeyFrame {
                    Cue = new Cue(1d),
                    Setters = {
                        new Setter(TranslateTransform.XProperty, 0d),
                        new Setter(TranslateTransform.YProperty, 0d)
                    }
                }
            }
        };

        _isAnimatingOrDelayed = true;
        _delayCancellation = new CancellationTokenSource();
        Console.WriteLine($"ACTUAL  FROM ({(int)actualFromOffset.X},{(int)actualFromOffset.Y})");
        Console.WriteLine("TO           (0,0)");
        Dispatcher.UIThread.Post(
            async void () => {
                try {
                    if (_delayCancellation.IsCancellationRequested)
                        return;
                    try {
                        await animation1.RunAsync(target, _delayCancellation.Token).ConfigureAwait(true);
                    } catch (OperationCanceledException) { } finally {
                        if (!_delayCancellation.IsCancellationRequested)
                            _isAnimatingOrDelayed = false;
                    }
                } catch (Exception ex) {
                    await LoggerService.ErrorAsync("OffsetAnimation异常", ex).ConfigureAwait(true);
                }
            },
            DispatcherPriority.Render);
    }

    private void CancelCurrent() {
        _delayCancellation?.Cancel();
        _delayCancellation?.Dispose();
        _delayCancellation = null;
    }

    public void Dispose() => CancelCurrent();
}

/// <summary>
/// 为每个 Visual 管理所有动画的上下文
/// </summary>
internal class RenderAnimationContext(Visual target) : IDisposable {
    private readonly Dictionary<IRenderAnimation, AnimationRunner> _runners = new();
    private Rect _lastBounds;
    private bool _hasBounds;

    public void OnBoundsChanged(Rect newBounds) {
        if (!_hasBounds) {
            _lastBounds = newBounds;
            _hasBounds = true;
            return;
        }

        if (_lastBounds == newBounds)
            return;
        Rect oldBounds = _lastBounds;
        _lastBounds = newBounds;
        // Console.WriteLine($"({oldBounds.Position})->({newBounds.Position})");

        RenderAnimations? animations = target.GetValue(RenderAnimation.RenderAnimationsProperty);

        if (animations is null)
            return;

        foreach (var anim in animations) {
            if (anim is OffsetRenderAnimation offsetAnim && oldBounds.Position != newBounds.Position) {
                var layoutDelta = oldBounds.Position - newBounds.Position;
                Console.WriteLine($"({oldBounds.Position})->({newBounds.Position})");
                GetRunner(offsetAnim).StartOffsetAnimation(layoutDelta);
            } else if (anim is SizeRenderAnimation sizeAnim && oldBounds.Size != newBounds.Size) {
                double fromScaleX = oldBounds.Width / newBounds.Width;
                double fromScaleY = oldBounds.Height / newBounds.Height;
                if (double.IsNaN(fromScaleX) || double.IsInfinity(fromScaleX))
                    fromScaleX = 1;
                if (double.IsNaN(fromScaleY) || double.IsInfinity(fromScaleY))
                    fromScaleY = 1;
                GetRunner(sizeAnim).StartSizeAnimation(new Size(fromScaleX, fromScaleY), new Size(1, 1));
            }
        }
    }

    private Rect GetLayoutBounds(Rect bounds) {
        if (TopLevel.GetTopLevel(target) is { } root) {
            Point? translated = target.TranslatePoint(bounds.Position, root);
            if (translated.HasValue) {
                double offsetX = target.GetValue(TranslateTransform.XProperty);
                double offsetY = target.GetValue(TranslateTransform.YProperty);
                return new Rect(new Point(translated.Value.X - offsetX, translated.Value.Y - offsetY), bounds.Size);
            }
        }

        return bounds;
    }

    private AnimationRunner GetRunner(IRenderAnimation animation) {
        if (!_runners.TryGetValue(animation, out var runner)) {
            runner = new AnimationRunner(target, animation);
            _runners[animation] = runner;
        }

        return runner;
    }

    public void Dispose() {
        foreach (var runner in _runners.Values)
            runner.Dispose();
        _runners.Clear();
    }
}

/// <summary>
/// 附加属性定义，使用全局类处理程序监听 Bounds 变化
/// </summary>
public static class RenderAnimation {
    public static readonly AttachedProperty<RenderAnimations?> RenderAnimationsProperty =
        AvaloniaProperty.RegisterAttached<Visual, RenderAnimations?>("RenderAnimations", typeof(RenderAnimation));

    private static readonly ConditionalWeakTable<Visual, RenderAnimationContext> _contexts = new();

    static RenderAnimation() {
        // 全局注册 BoundsProperty 的类处理程序
        Visual.BoundsProperty.Changed.AddClassHandler<Visual>((visual, e) => {
            // 仅处理有 RenderAnimations 附加属性的 Visual
            var animations = visual.GetValue(RenderAnimationsProperty);
            if (animations is null || animations.Count == 0)
                return;

            var context = _contexts.GetValue(visual, v => new RenderAnimationContext(v));
            if (e.NewValue is Rect newBounds)
                context.OnBoundsChanged(newBounds);
        });
    }

    public static void SetRenderAnimations(Visual element, RenderAnimations value) {
        element.SetValue(RenderAnimationsProperty, value);
    }

    public static RenderAnimations? GetRenderAnimations(Visual element) {
        return element.GetValue(RenderAnimationsProperty);
    }

    // 可选：清理上下文（当附加属性被清除时）
    private static void OnRenderAnimationsChanged(Visual visual, AvaloniaPropertyChangedEventArgs e) {
        if (e.NewValue == null && _contexts.TryGetValue(visual, out var context)) {
            context.Dispose();
            _contexts.Remove(visual);
            visual.RenderTransform = null;
        }
    }
}