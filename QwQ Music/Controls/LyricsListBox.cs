using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace QwQ_Music.Controls;

/// <summary>
/// 基于ListBox的歌词控件，支持虚拟化和平滑滚动
/// </summary>
public class LyricsListBox : ListBox
{
    #region 依赖属性

    // 滚动动画持续时间
    public static readonly StyledProperty<TimeSpan> ScrollAnimationDurationProperty = AvaloniaProperty.Register<
        LyricsListBox,
        TimeSpan
    >(nameof(ScrollAnimationDuration), TimeSpan.FromMilliseconds(500));

    // 滚动缓动函数
    public static readonly StyledProperty<Easing> ScrollEasingProperty = AvaloniaProperty.Register<
        LyricsListBox,
        Easing
    >(nameof(ScrollEasing), new CubicEaseOut());

    // 用户滚动超时时间
    public static readonly StyledProperty<TimeSpan> UserScrollTimeoutProperty = AvaloniaProperty.Register<
        LyricsListBox,
        TimeSpan
    >(nameof(UserScrollTimeout), TimeSpan.FromSeconds(3));

    // 是否启用自动滚动
    public static readonly StyledProperty<bool> AutoScrollEnabledProperty = AvaloniaProperty.Register<
        LyricsListBox,
        bool
    >(nameof(AutoScrollEnabled), true);

    #endregion

    #region 属性
    
    public TimeSpan ScrollAnimationDuration
    {
        get => GetValue(ScrollAnimationDurationProperty);
        set => SetValue(ScrollAnimationDurationProperty, value);
    }

    public Easing ScrollEasing
    {
        get => GetValue(ScrollEasingProperty);
        set => SetValue(ScrollEasingProperty, value);
    }

    public TimeSpan UserScrollTimeout
    {
        get => GetValue(UserScrollTimeoutProperty);
        set => SetValue(UserScrollTimeoutProperty, value);
    }

    public bool AutoScrollEnabled
    {
        get => GetValue(AutoScrollEnabledProperty);
        set => SetValue(AutoScrollEnabledProperty, value);
    }

    #endregion

    #region 私有字段

    private ScrollViewer? _scrollViewer;
    private bool _isUserScrolling;
    private DateTime _lastUserScrollTime = DateTime.MinValue;
    private CancellationTokenSource? _scrollAnimationCts;
    private int _lastScrolledIndex = -1;
    private bool _isScrolling;

    #endregion

    static LyricsListBox()
    {
        SelectionModeProperty.OverrideDefaultValue<LyricsListBox>(SelectionMode.Multiple);
        SelectedIndexProperty.Changed.AddClassHandler<LyricsListBox>((x, e) => x.OnSelectedIndexChanged(e));
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        // 解除之前的事件绑定
        UnsubscribeEvents();

        // 获取ScrollViewer
        _scrollViewer = this.FindDescendantOfType<ScrollViewer>();

        // 绑定事件
        SubscribeEvents();
    }

    private void UnsubscribeEvents()
    {
        if (_scrollViewer != null)
        {
            _scrollViewer.ScrollChanged -= ScrollViewer_ScrollChanged;
        }
    }

    private void SubscribeEvents()
    {
        if (_scrollViewer != null)
        {
            _scrollViewer.ScrollChanged += ScrollViewer_ScrollChanged;
        }
    }

    /// <summary>
    /// 处理滚动事件
    /// </summary>
    private void ScrollViewer_ScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        // 忽略由于内容或视口大小变化引起的滚动事件
        if (e.ExtentDelta != Vector.Zero || e.ViewportDelta != Vector.Zero)
            return;

        // 检测用户滚动: 仅当滚动是由用户发起（而非程序化滚动动画）且有明显Y轴偏移时触发
        if (Math.Abs(e.OffsetDelta.Y) <= 0.1 || _isScrolling || _scrollAnimationCts != null) 
            return;
        
        _lastUserScrollTime = DateTime.Now;
        _isUserScrolling = true;
    }

    /// <summary>
    /// 当选中索引变化时
    /// </summary>
    private void OnSelectedIndexChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (!AutoScrollEnabled) return;
        
        // 检查用户滚动超时
        if (_isUserScrolling && DateTime.Now - _lastUserScrollTime > UserScrollTimeout)
        {
            _isUserScrolling = false;
        }
         
        // 如果不是用户滚动，且选中索引有效，且与上次滚动的索引不同，则滚动到当前歌词
        int newIndex = (int)e.NewValue!;
        if (_isUserScrolling || newIndex < 0 || newIndex == _lastScrolledIndex)
            return;
        
        _lastScrolledIndex = newIndex;
        ScrollToSelectedItemAsync();
    }

    /// <summary>
    /// 滚动到选中项
    /// </summary>
    private async void ScrollToSelectedItemAsync()
    {
        _isScrolling = true;
        for (int i = 0; i < 3; i++)
        {
            if (_scrollViewer == null || SelectedIndex < 0 || ItemCount == 0) return;

            // 获取选中的容器
            if (ContainerFromIndex(SelectedIndex) is ListBoxItem container)
            {
                // 异步执行滚动动画
                await ScrollToContainerAsync(container);
                break;
            }

            // 如果item被虚拟化先使其出现在视图中
            ScrollIntoView(SelectedIndex);

            // 给UI一点时间来更新
            await Task.Delay(10);

        }
        _isScrolling = false;
    }

    /// <summary>
    /// 异步滚动到指定容器
    /// </summary>
    private async Task ScrollToContainerAsync(ListBoxItem container)
    {
        if (_scrollViewer == null)
            return;

        // 取消正在进行的滚动动画
        _scrollAnimationCts?.Cancel();
        _scrollAnimationCts = new CancellationTokenSource();
        var cancellationToken = _scrollAnimationCts.Token;

        try
        {

            
            // 获取容器相对于ScrollViewer的位置
            var containerBounds = container.Bounds;
            var scrollViewerBounds = _scrollViewer.Bounds;
            
            // 计算容器中心点相对于ScrollViewer的位置
            double containerTopInScrollViewer = container.TranslatePoint(new Point(0, 0), _scrollViewer)?.Y ?? 0;
            double containerHeight = containerBounds.Height;
            
            // 计算目标偏移量，使容器居中
            double viewportHeight = scrollViewerBounds.Height;
            double targetOffset = containerTopInScrollViewer - (viewportHeight - containerHeight) / 2 + _scrollViewer.Offset.Y;
            
            // 确保目标偏移量在有效范围内
            targetOffset = Math.Max(0, Math.Min(targetOffset, _scrollViewer.Extent.Height - viewportHeight));
            
            double currentOffset = _scrollViewer.Offset.Y;

            // 如果差异很小，直接设置
            if (Math.Abs(targetOffset - currentOffset) < 1)
            {
                _scrollViewer.Offset = new Vector(_scrollViewer.Offset.X, targetOffset);
                return;
            }

            // 创建并执行滚动动画
            var animation = new Animation
            {
                Duration = ScrollAnimationDuration,
                FillMode = FillMode.Forward,
                Easing = ScrollEasing,
            };

            animation.Children.Add(
                new KeyFrame
                {
                    Cue = new Cue(0d),
                    Setters =
                    {
                        new Setter(ScrollViewer.OffsetProperty, new Vector(_scrollViewer.Offset.X, currentOffset)),
                    },
                }
            );

            animation.Children.Add(
                new KeyFrame
                {
                    Cue = new Cue(1d),
                    Setters =
                    {
                        new Setter(ScrollViewer.OffsetProperty, new Vector(_scrollViewer.Offset.X, targetOffset)),
                    },
                }
            );

            await animation.RunAsync(_scrollViewer, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // 动画被取消，忽略异常
        }
        catch (Exception ex)
        {
            // 记录异常，但不抛出
            System.Diagnostics.Debug.WriteLine($"滚动动画异常: {ex.Message}");
        }
        finally
        {
            // 只有当这是最后一个动画时才重置标志
            if (!cancellationToken.IsCancellationRequested)
            {
                _scrollAnimationCts?.Cancel();
                _scrollAnimationCts = null;
            }
        }
    }
    
    /// <summary>
    /// 手动触发滚动到当前选中项
    /// </summary>
    public void ForceScrollToCurrentItem()
    {
        if (SelectedIndex >= 0)
        {
            _isUserScrolling = false;
            _lastScrolledIndex = -1; // 重置以确保滚动发生
            ScrollToSelectedItemAsync();
        }
    }
    
    /// <summary>
    /// 清理资源
    /// </summary>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        
        // 清理资源
        _scrollAnimationCts?.Cancel();
        _scrollAnimationCts?.Dispose();
        _scrollAnimationCts = null;
 
        UnsubscribeEvents();
    }
}