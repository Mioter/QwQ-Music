using System;
using System.Threading;
using System.Threading.Tasks;
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
using QwQ.Avalonia.Helper;

namespace QwQ_Music.Helper.Behaviors;

public class ScrollToItemBehavior
{
    // 用于跟踪和取消正在进行的滚动动画
    private static CancellationTokenSource? currentScrollCts;

    public static readonly AttachedProperty<object> ScrollToItemProperty = AvaloniaProperty.RegisterAttached<
        ScrollToItemBehavior,
        Control,
        object
    >("ScrollToItem", null!, false, BindingMode.TwoWay);

    // 滚动动画持续时间
    public static readonly AttachedProperty<TimeSpan> ScrollDurationProperty = AvaloniaProperty.RegisterAttached<
        ScrollToItemBehavior,
        Control,
        TimeSpan
    >("ScrollDuration", TimeSpan.FromMilliseconds(300));

    // 是否启用平滑滚动
    public static readonly AttachedProperty<bool> SmoothScrollingEnabledProperty = AvaloniaProperty.RegisterAttached<
        ScrollToItemBehavior,
        Control,
        bool
    >("SmoothScrollingEnabled", true);

    // 滚动缓动函数
    public static readonly AttachedProperty<Easing> ScrollEasingProperty = AvaloniaProperty.RegisterAttached<
        ScrollToItemBehavior,
        Control,
        Easing
    >("ScrollEasing", new LinearEasing());

    static ScrollToItemBehavior()
    {
        ScrollToItemProperty.Changed.Subscribe(args =>
        {
            switch (args.Sender)
            {
                case DataGrid dataGrid:
                {
                    // DataGrid 没有 ContainerFromItem 方法，暂不实现平滑滚动。
                    object item = args.NewValue.Value;
                    Dispatcher.UIThread.Post(() => dataGrid.ScrollIntoView(item, null));
                    break;
                }
                case ListBox listBox:
                {
                    object item = args.NewValue.Value;
                    bool smoothScrollingEnabled = GetSmoothScrollingEnabled(listBox);

                    // 取消当前正在进行的滚动动画
                    CancelCurrentScrollAnimation();

                    if (smoothScrollingEnabled)
                    {
                        var duration = GetScrollDuration(listBox);
                        var easing = GetScrollEasing(listBox);

                        // 创建新的取消令牌
                        currentScrollCts = new CancellationTokenSource();
                        var token = currentScrollCts.Token;

                        Dispatcher.UIThread.Post(async void () =>
                        {
                            /*// 确保项目可见 为保证滚动效果，先不使用
                            listBox.ScrollIntoView(item);*/
                            try
                            {
                                await SmoothScrollToItemCenterAsync(listBox, item, duration, easing, token);
                            }
                            catch (Exception)
                            {
                                // ignored
                            }
                            finally
                            {
                                if (!token.IsCancellationRequested)
                                {
                                    CancelCurrentScrollAnimation();
                                }
                            }
                        });
                    }
                    else
                    {
                        Dispatcher.UIThread.Post(() => listBox.ScrollIntoView(item));
                    }
                    break;
                }
            }
        });
    }

    // 取消当前正在进行的滚动动画
    private static void CancelCurrentScrollAnimation()
    {
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
        CancellationToken cancellationToken
    )
    {
        // 获取滚动查看器
        var scrollViewer = control.FindDescendantOfType<ScrollViewer>();
        if (scrollViewer == null)
            return;

        ItemsControl? itemsControl = null;
        Control? container = null;

        if (control is ListBox listBox)
        {
            itemsControl = listBox;
            container = listBox.ContainerFromItem(item);
        }

        if (container == null || itemsControl == null)
            return;

        // 计算目标滚动位置
        var itemBounds = container.Bounds;
        var scrollViewerBounds = scrollViewer.Bounds;

        if (scrollViewer.VerticalScrollBarVisibility != ScrollBarVisibility.Disabled)
        {
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
                cancellationToken
            );
        }
        else if (scrollViewer.HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled)
        {
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
                cancellationToken
            );
        }
    }

    private static async Task AnimateScrollOffsetAsync(
        ScrollViewer scrollViewer,
        double fromValue,
        double toValue,
        TimeSpan duration,
        Easing easing,
        Orientation orientation,
        CancellationToken cancellationToken
    )
    {
        // 创建动画
        var animation = new Animation
        {
            Duration = duration,
            FillMode = FillMode.Forward,
            Easing = easing,
        };

        // 添加关键帧
        animation.Children.Add(
            new KeyFrame
            {
                Cue = new Cue(0d),
                Setters =
                {
                    new Setter(
                        ScrollViewer.OffsetProperty,
                        orientation == Orientation.Vertical
                            ? new Vector(scrollViewer.Offset.X, fromValue)
                            : new Vector(fromValue, scrollViewer.Offset.Y)
                    ),
                },
            }
        );

        animation.Children.Add(
            new KeyFrame
            {
                Cue = new Cue(1d),
                Setters =
                {
                    new Setter(
                        ScrollViewer.OffsetProperty,
                        orientation == Orientation.Vertical
                            ? new Vector(scrollViewer.Offset.X, toValue)
                            : new Vector(toValue, scrollViewer.Offset.Y)
                    ),
                },
            }
        );

        // 创建一个TaskCompletionSource来跟踪动画完成

        // 运行动画
        await animation.RunAsync(scrollViewer, cancellationToken);
    }

    #region 属性访问器

    public static void SetScrollToItem(Control element, object value)
    {
        element.SetValue(ScrollToItemProperty, value);
    }

    public static object GetScrollToItem(Control element)
    {
        return element.GetValue(ScrollToItemProperty);
    }

    public static void SetScrollDuration(Control element, TimeSpan value)
    {
        element.SetValue(ScrollDurationProperty, value);
    }

    public static TimeSpan GetScrollDuration(Control element)
    {
        return element.GetValue(ScrollDurationProperty);
    }

    public static void SetSmoothScrollingEnabled(Control element, bool value)
    {
        element.SetValue(SmoothScrollingEnabledProperty, value);
    }

    public static bool GetSmoothScrollingEnabled(Control element)
    {
        return element.GetValue(SmoothScrollingEnabledProperty);
    }

    public static void SetScrollEasing(Control element, Easing value)
    {
        element.SetValue(ScrollEasingProperty, value);
    }

    public static Easing GetScrollEasing(Control element)
    {
        return element.GetValue(ScrollEasingProperty);
    }

    #endregion
}
