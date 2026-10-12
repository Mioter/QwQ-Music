using System.Collections;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using QwQ_Music.Common.Services;

namespace QwQ_Music.UI.Behaviors;

public class ScrollToItemBehavior {
    // 用于跟踪和取消正在进行的滚动动画
    private static CancellationTokenSource? currentScrollCts;

    public static readonly AttachedProperty<object> ScrollToItemProperty =
        AvaloniaProperty.RegisterAttached<ScrollToItemBehavior, Control, object>(
            "ScrollToItem",
            new object(),
            false,
            BindingMode.TwoWay);

    // 滚动动画持续时间
    public static readonly AttachedProperty<TimeSpan> ScrollDurationProperty =
        AvaloniaProperty.RegisterAttached<ScrollToItemBehavior, Control, TimeSpan>(
            "ScrollDuration",
            TimeSpan.FromMilliseconds(300));

    // 是否启用平滑滚动
    public static readonly AttachedProperty<bool> SmoothScrollingEnabledProperty =
        AvaloniaProperty.RegisterAttached<ScrollToItemBehavior, Control, bool>("SmoothScrollingEnabled", true);

    // 滚动缓动函数
    public static readonly AttachedProperty<Easing> ScrollEasingProperty =
        AvaloniaProperty.RegisterAttached<ScrollToItemBehavior, Control, Easing>("ScrollEasing", new LinearEasing());

    static ScrollToItemBehavior() { ScrollToItemProperty.Changed.AddClassHandler<Control>(OnScrollToItemChanged); }

    private static void OnScrollToItemChanged(Control control, AvaloniaPropertyChangedEventArgs args) {
        object? newValue = args.NewValue;

        if (newValue == null)
            return;

        switch (control) {
            case DataGrid dataGrid: {
                // DataGrid 没有 ContainerFromItem 方法，暂不实现平滑滚动。
                object item = newValue;
                Dispatcher.UIThread.Post(() => dataGrid.ScrollIntoView(item, null));

                break;
            }
            case ListBox listBox: {
                object item = newValue;
                bool smoothScrollingEnabled = GetSmoothScrollingEnabled(listBox);

                // 取消当前正在进行的滚动动画
                CancelCurrentScrollAnimation();

                if (smoothScrollingEnabled) {
                    TimeSpan duration = GetScrollDuration(listBox);
                    Easing easing = GetScrollEasing(listBox);

                    // 创建新的令牌
                    currentScrollCts = new CancellationTokenSource();
                    CancellationToken token = currentScrollCts.Token;

                    Dispatcher.UIThread.InvokeAsync(async Task () => {
                                  try {
                                      await SmoothScrollToItemCenterAsync(listBox, item, duration, easing, token)
                                          .ConfigureAwait(true);
                                  } catch (OperationCanceledException) {
                                      // 动画被取消，忽略异常
                                  } catch (Exception ex) {
                                      await LoggerService
                                            .WarningAsync($"动画期间出现{ex.GetType()}:{ex.Message}\n{ex.StackTrace}")
                                            .ConfigureAwait(true);
                                  } finally {
                                      if (!token.IsCancellationRequested)
                                          CancelCurrentScrollAnimation();
                                  }
                              })
                              // ReSharper disable once MethodSupportsCancellation
                              .ContinueWith(LoggerService.HandleException)
                              .ConfigureAwait(false);
                } else {
                    listBox.ScrollIntoView(item);
                }

                break;
            }
        }
    }

    // 取消当前正在进行的滚动动画
    private static void CancelCurrentScrollAnimation() {
        if (currentScrollCts is not { IsCancellationRequested: false })
            return;

        currentScrollCts.Cancel();
        currentScrollCts.Dispose();
        currentScrollCts = null;
    }

    private static async Task SmoothScrollToItemCenterAsync(
        Control control,
        object item,
        TimeSpan duration,
        Easing easing,
        CancellationToken cancellationToken) {
        // 获取滚动查看器
        var scrollViewer = control.FindDescendantOfType<ScrollViewer>();

        if (scrollViewer == null)
            return;

        ItemsControl? itemsControl = null;
        Control? container = null;

        if (control is ListBox listBox) {
            itemsControl = listBox;
            container = listBox.ContainerFromItem(item);
        }

        if (container == null || itemsControl == null)
            return;

        // 计算目标滚动位置
        Rect itemBounds = container.Bounds;
        Rect scrollViewerBounds = scrollViewer.Bounds;

        if (scrollViewer.VerticalScrollBarVisibility != ScrollBarVisibility.Disabled) {
            // 垂直滚动
            double targetOffset = itemBounds.Y + itemBounds.Height / 2 - scrollViewerBounds.Height / 2;
            targetOffset = Math.Max(0, Math.Min(targetOffset, scrollViewer.ScrollBarMaximum.Y));

            // 使用Avalonia动画系统
            await AnimateScrollOffsetAsync(
                    scrollViewer,
                    scrollViewer.Offset.Y,
                    targetOffset,
                    duration,
                    easing,
                    Orientation.Vertical,
                    cancellationToken)
                .ConfigureAwait(false);
        } else if (scrollViewer.HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled) {
            // 水平滚动
            double targetOffset = itemBounds.X + itemBounds.Width / 2 - scrollViewerBounds.Width / 2;
            targetOffset = Math.Max(0, Math.Min(targetOffset, scrollViewer.ScrollBarMaximum.X));

            // 使用Avalonia动画系统
            await AnimateScrollOffsetAsync(
                    scrollViewer,
                    scrollViewer.Offset.X,
                    targetOffset,
                    duration,
                    easing,
                    Orientation.Horizontal,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private static async Task AnimateScrollOffsetAsync(
        ScrollViewer scrollViewer,
        double fromValue,
        double toValue,
        TimeSpan duration,
        Easing easing,
        Orientation orientation,
        CancellationToken cancellationToken) {
        // 创建动画
        var animation = new Animation { Duration = duration, FillMode = FillMode.Forward, Easing = easing };

        // 添加关键帧
        animation.Children.Add(
            new KeyFrame {
                Cue = new Cue(0d),
                Setters = {
                    new Setter(
                        ScrollViewer.OffsetProperty,
                        orientation == Orientation.Vertical ?
                            new Vector(scrollViewer.Offset.X, fromValue) :
                            new Vector(fromValue, scrollViewer.Offset.Y))
                }
            });

        animation.Children.Add(
            new KeyFrame {
                Cue = new Cue(1d),
                Setters = {
                    new Setter(
                        ScrollViewer.OffsetProperty,
                        orientation == Orientation.Vertical ?
                            new Vector(scrollViewer.Offset.X, toValue) :
                            new Vector(toValue, scrollViewer.Offset.Y))
                }
            });

        // 运行动画
        await animation.RunAsync(scrollViewer, cancellationToken);
    }

    #region 属性访问器

    public static void SetScrollToItem(Control element, object value) {
        if (value is IEnumerable collection) {
            IEnumerator enumerator = collection.GetEnumerator();
            if (enumerator.Current is { } val)
                element.SetValue(ScrollToItemProperty, val);
            ((IDisposable)enumerator).Dispose();
        } else {
            element.SetValue(ScrollToItemProperty, value);
        }
    }

    public static object GetScrollToItem(Control element) { return element.GetValue(ScrollToItemProperty); }

    public static void SetScrollDuration(Control element, TimeSpan value) {
        element.SetValue(ScrollDurationProperty, value);
    }

    public static TimeSpan GetScrollDuration(Control element) { return element.GetValue(ScrollDurationProperty); }

    public static void SetSmoothScrollingEnabled(Control element, bool value) {
        element.SetValue(SmoothScrollingEnabledProperty, value);
    }

    public static bool GetSmoothScrollingEnabled(Control element) {
        return element.GetValue(SmoothScrollingEnabledProperty);
    }

    public static void SetScrollEasing(Control element, Easing value) { element.SetValue(ScrollEasingProperty, value); }

    public static Easing GetScrollEasing(Control element) { return element.GetValue(ScrollEasingProperty); }

    #endregion
}