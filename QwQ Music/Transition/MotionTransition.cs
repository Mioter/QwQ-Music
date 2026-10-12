using Avalonia;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;

namespace QwQ_Music.Transition;

public class MotionTransitions {
    public static readonly AttachedProperty<bool> OffsetTransitionProperty =
        AvaloniaProperty.RegisterAttached<MotionTransitions, Visual, bool>("OffsetTransition");

    public static readonly AttachedProperty<bool> SizeTransitionProperty =
        AvaloniaProperty.RegisterAttached<MotionTransitions, Visual, bool>("SizeTransition");

    public static readonly AttachedProperty<TimeSpan> OffsetDurationProperty =
        AvaloniaProperty.RegisterAttached<MotionTransitions, Visual, TimeSpan>("OffsetDuration");

    public static readonly AttachedProperty<TimeSpan> OffsetDelayProperty =
        AvaloniaProperty.RegisterAttached<MotionTransitions, Visual, TimeSpan>("OffsetDelay");

    public static readonly AttachedProperty<TimeSpan> SizeDurationProperty =
        AvaloniaProperty.RegisterAttached<MotionTransitions, Visual, TimeSpan>("SizeDuration");

    public static readonly AttachedProperty<TimeSpan> SizeDelayProperty =
        AvaloniaProperty.RegisterAttached<MotionTransitions, Visual, TimeSpan>("SizeDelay");

    // 监听属性变化
    static MotionTransitions() {
        OffsetTransitionProperty.Changed.AddClassHandler<Visual, bool>(OnTransitionChanged);
        SizeTransitionProperty.Changed.AddClassHandler<Visual, bool>(OnTransitionChanged);
        OffsetDurationProperty.Changed.AddClassHandler<Visual, TimeSpan>(OnDurationChanged);
        SizeDurationProperty.Changed.AddClassHandler<Visual, TimeSpan>(OnDurationChanged);
        OffsetDelayProperty.Changed.AddClassHandler<Visual, TimeSpan>(OnDelayChanged);
        SizeDelayProperty.Changed.AddClassHandler<Visual, TimeSpan>(OnDelayChanged);
    }

    private static void OnTransitionChanged(Visual? target, AvaloniaPropertyChangedEventArgs<bool> args) {
        if (target is null)
            return;

        ApplyImplicitAnimations(target);
    }

    private static void OnDurationChanged(Visual? target, AvaloniaPropertyChangedEventArgs<TimeSpan> args) {
        if (target is null)
            return;

        if (!GetOffsetTransition(target) && !GetSizeTransition(target))
            return;

        ApplyImplicitAnimations(target);
    }

    private static void OnDelayChanged(Visual? target, AvaloniaPropertyChangedEventArgs<TimeSpan> args) {
        if (target is null)
            return;

        if (!GetOffsetTransition(target) && !GetSizeTransition(target))
            return;

        ApplyImplicitAnimations(target);
    }

    private static void ApplyImplicitAnimations(Visual target) {
        var visual = ElementComposition.GetElementVisual(target);
        if (visual?.Compositor == null) {
            TryApplyWhenAttached(target);
            return;
        }

        var compositor = visual.Compositor;
        visual.ImplicitAnimations ??= compositor.CreateImplicitAnimationCollection();

        if (GetOffsetTransition(target)) {
            visual.ImplicitAnimations["Offset"] = PredefinedAnimations.CreateOffsetAnimation(
                compositor,
                GetOffsetDuration(target),
                GetOffsetDelay(target));
        } else {
            visual.ImplicitAnimations.Remove("Offset");
        }

        if (GetSizeTransition(target)) {
            visual.ImplicitAnimations["Size"] = PredefinedAnimations.CreateSizeAnimation(
                compositor,
                GetSizeDuration(target),
                GetSizeDelay(target));
        } else {
            visual.ImplicitAnimations.Remove("Size");
        }

        if (visual.ImplicitAnimations.Count == 0) {
            visual.ImplicitAnimations = null;
        }
    }

    private static void TryApplyWhenAttached(Visual target) {
        // 属性可能在元素附加到可视树之前设置，附加后补一次应用。
        void AttachedHandler(object? sender, VisualTreeAttachmentEventArgs args) {
            target.AttachedToVisualTree -= AttachedHandler;
            ApplyImplicitAnimations(target);
        }

        target.AttachedToVisualTree -= AttachedHandler;
        target.AttachedToVisualTree += AttachedHandler;
    }

    public static void SetOffsetTransition(Visual element, bool value) =>
        element.SetValue(OffsetTransitionProperty, value);

    public static bool GetOffsetTransition(Visual element) => element.GetValue(OffsetTransitionProperty);

    public static void SetSizeTransition(Visual element, bool value) => element.SetValue(SizeTransitionProperty, value);

    public static bool GetSizeTransition(Visual element) => element.GetValue(SizeTransitionProperty);

    public static void SetOffsetDuration(Visual element, TimeSpan value) =>
        element.SetValue(OffsetDurationProperty, value);

    public static TimeSpan GetOffsetDuration(Visual element) => element.GetValue(OffsetDurationProperty);

    public static void SetOffsetDelay(Visual element, TimeSpan value) => element.SetValue(OffsetDelayProperty, value);

    public static TimeSpan GetOffsetDelay(Visual element) => element.GetValue(OffsetDelayProperty);

    public static void SetSizeDuration(Visual element, TimeSpan value) => element.SetValue(SizeDurationProperty, value);

    public static TimeSpan GetSizeDuration(Visual element) => element.GetValue(SizeDurationProperty);

    public static void SetSizeDelay(Visual element, TimeSpan value) => element.SetValue(SizeDelayProperty, value);

    public static TimeSpan GetSizeDelay(Visual element) => element.GetValue(SizeDelayProperty);
}

public static class PredefinedAnimations {
    public static KeyFrameAnimation CreateOffsetAnimation(
        Compositor compositor,
        TimeSpan duration,
        TimeSpan delay = default) {
        // 位置变化动画
        var offsetAnimation = compositor.CreateVector3KeyFrameAnimation();
        offsetAnimation.Duration = duration;
        if (delay != TimeSpan.Zero)
            offsetAnimation.DelayTime = delay;
        offsetAnimation.Target = "Offset";
        offsetAnimation.InsertExpressionKeyFrame(1.0f, "this.FinalValue");

        return offsetAnimation;
    }

    public static KeyFrameAnimation CreateSizeAnimation(
        Compositor compositor,
        TimeSpan duration,
        TimeSpan delay = default) {
        // 尺寸变化动画
        var sizeAnimation = compositor.CreateVector2KeyFrameAnimation();
        sizeAnimation.Duration = duration;
        if (delay != TimeSpan.Zero)
            sizeAnimation.DelayTime = delay;
        sizeAnimation.Target = "Size";
        sizeAnimation.InsertExpressionKeyFrame(1.0f, "this.FinalValue");
        return sizeAnimation;
    }
}