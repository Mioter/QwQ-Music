using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;

namespace QwQ_Music.UI.Controls.AnchorControl;

public class Anchor : ItemsControl
{
    public static readonly StyledProperty<SelectingItemsControl?> IndexSourceProperty =
        AvaloniaProperty.Register<Anchor, SelectingItemsControl?>(nameof(IndexSource));

    public static readonly StyledProperty<bool> AnimatedScrollProperty =
        AvaloniaProperty.Register<Anchor, bool>(nameof(AnimatedScroll), true);

    public static readonly StyledProperty<TimeSpan> ScrollDurationProperty =
        AvaloniaProperty.Register<Anchor, TimeSpan>(nameof(ScrollDuration), TimeSpan.FromMilliseconds(300));

    public static readonly StyledProperty<Easing> ScrollEasingProperty =
        AvaloniaProperty.Register<Anchor, Easing>(nameof(ScrollEasing), new LinearEasing());

    public static readonly StyledProperty<object?> StickyHeaderProperty =
        AvaloniaProperty.Register<Anchor, object?>(nameof(StickyHeader));

    public static readonly StyledProperty<double> HeaderHeightProperty = AnchorItem.HeaderHeightProperty.AddOwner<Anchor>();

    public static readonly StyledProperty<IDataTemplate?> HeaderTemplateProperty = AnchorItem.HeaderTemplateProperty.AddOwner<Anchor>();

    private bool _disableSync;
    private bool _isSyncing;
    private CancellationTokenSource? _scrollCts;

    private ScrollViewer? _scrollViewer;
    private Control? _stickyHeader;

    public SelectingItemsControl? IndexSource
    {
        get => GetValue(IndexSourceProperty);
        set => SetValue(IndexSourceProperty, value);
    }

    public bool AnimatedScroll
    {
        get => GetValue(AnimatedScrollProperty);
        set => SetValue(AnimatedScrollProperty, value);
    }

    public TimeSpan ScrollDuration
    {
        get => GetValue(ScrollDurationProperty);
        set => SetValue(ScrollDurationProperty, value);
    }

    public Easing ScrollEasing
    {
        get => GetValue(ScrollEasingProperty);
        set => SetValue(ScrollEasingProperty, value);
    }

    public object? StickyHeader
    {
        get => GetValue(StickyHeaderProperty);
        set => SetValue(StickyHeaderProperty, value);
    }

    public double HeaderHeight
    {
        get => GetValue(HeaderHeightProperty);
        set => SetValue(HeaderHeightProperty, value);
    }

    public IDataTemplate? HeaderTemplate
    {
        get => GetValue(HeaderTemplateProperty);
        set => SetValue(HeaderTemplateProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _scrollViewer = e.NameScope.Find<ScrollViewer>("PART_ScrollViewer");
        _stickyHeader = e.NameScope.Find<Control>("PART_StickyHeader");

        if (_scrollViewer != null)
        {
            _scrollViewer.ScrollChanged += OnScrollChanged;
            _scrollViewer.SizeChanged += OnScrollViewerSizeChanged;
        }

        UpdateStickyHeader();
        UpdateLastItemMinHeight();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property != IndexSourceProperty)
            return;

        UnhookIndexSource();
        HookIndexSource();
    }

    private void HookIndexSource()
    {
        if (IndexSource != null)
            IndexSource.SelectionChanged += OnIndexSourceSelectionChanged;
    }

    private void UnhookIndexSource()
    {
        if (IndexSource != null)
            IndexSource.SelectionChanged -= OnIndexSourceSelectionChanged;
    }

    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        UpdateStickyHeader();

        if (_isSyncing || _disableSync) return;

        SyncListBoxSelection();
    }

    private void OnIndexSourceSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isSyncing) return;

        int? selectedIndex = IndexSource?.SelectedIndex;

        if (selectedIndex is not ({ } idx and >= 0) || idx >= Items.Count) return;

        _isSyncing = true;
        _disableSync = true;

        try
        {
            ScrollToAnchor(idx);
        }
        finally
        {
            _isSyncing = false;
        }
    }

    /// <summary>
    ///     查找当前应该吸附的锚点索引
    /// </summary>
    private int FindStickyIndex()
    {
        if (_scrollViewer == null || Items.Count == 0) return 0;

        int stickyIndex = 0;
        double minDistance = double.MaxValue;

        for (int i = 0; i < Items.Count; i++)
        {
            if (Items[i] is not AnchorItem item) continue;

            var point = item.TranslatePoint(new Point(0, 0), _scrollViewer);

            if (!point.HasValue) continue;

            double distance = Math.Abs(point.Value.Y);

            // 找到距离顶部最近且不超过顶部的项目
            if (!(point.Value.Y <= 0) || !(distance < minDistance))
                continue;

            minDistance = distance;
            stickyIndex = i;
        }

        return stickyIndex;
    }

    private void UpdateStickyHeader()
    {
        if (_scrollViewer == null || _stickyHeader == null) return;

        int stickyIndex = FindStickyIndex();

        // 更新粘性头部内容
        if (stickyIndex < Items.Count && Items[stickyIndex] is AnchorItem stickyItem)
        {
            StickyHeader = stickyItem.Header;
        }
        else
        {
            StickyHeader = null;
        }

        // 处理推出动画效果
        UpdateStickyHeaderAnimation(stickyIndex);
    }

    private void UpdateStickyHeaderAnimation(int stickyIndex)
    {
        if (_scrollViewer == null || _stickyHeader == null || stickyIndex + 1 >= Items.Count || Items[stickyIndex + 1] is not AnchorItem nextItem)
        {
            ResetStickyHeaderTransform();

            return;
        }

        var nextPoint = nextItem.TranslatePoint(new Point(0, 0), _scrollViewer);

        if (!nextPoint.HasValue)
        {
            ResetStickyHeaderTransform();

            return;
        }

        // 当下一个项目进入粘性头部区域时，应用推出动画
        if (nextPoint.Value.Y < _stickyHeader.Bounds.Height && nextPoint.Value.Y > 0)
        {
            _stickyHeader.RenderTransform = new TranslateTransform(0, nextPoint.Value.Y - _stickyHeader.Bounds.Height);
        }
        else
        {
            ResetStickyHeaderTransform();
        }
    }

    private void ResetStickyHeaderTransform()
    {
        if (_stickyHeader != null)
        {
            _stickyHeader.RenderTransform = new TranslateTransform(0, 0);
        }
    }

    private void SyncListBoxSelection()
    {
        if (IndexSource == null) return;

        int stickyIndex = FindStickyIndex();

        if (IndexSource.SelectedIndex == stickyIndex) return;

        _isSyncing = true;

        try
        {
            IndexSource.SelectedIndex = stickyIndex;
        }
        finally
        {
            _isSyncing = false;
        }
    }

    private void ScrollToAnchor(int index)
    {
        if (_scrollViewer == null || index < 0 || index >= Items.Count) return;
        if (Items[index] is not AnchorItem item) return;

        ScrollToItem(item, _scrollViewer);
    }

    private async void ScrollToItem(AnchorItem container, ScrollViewer scrollViewer)
    {
        try
        {
            var point = container.TranslatePoint(new Point(0, 0), scrollViewer);

            if (!point.HasValue) return;

            double from = scrollViewer.Offset.Y;
            double to = from + point.Value.Y;

            if (Math.Abs(to - from) < 1)
            {
                scrollViewer.Offset = new Vector(scrollViewer.Offset.X, to);

                return;
            }

            if (AnimatedScroll)
            {
                await AnimateScrollAsync(scrollViewer, to);
            }
            else
            {
                scrollViewer.Offset = new Vector(scrollViewer.Offset.X, to);
            }
        }
        catch (Exception)
        {
            // 忽略异常
        }
        finally
        {
            _disableSync = false;
        }
    }

    private async Task AnimateScrollAsync(ScrollViewer scrollViewer, double to)
    {
        if (_scrollCts != null)
        {
            await _scrollCts.CancelAsync();
        }

        _scrollCts = new CancellationTokenSource();
        var token = _scrollCts.Token;

        var animation = new Animation
        {
            Duration = ScrollDuration,
            Easing = ScrollEasing,
            FillMode = FillMode.Forward,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(1d),
                    Setters =
                    {
                        new Setter(ScrollViewer.OffsetProperty, new Vector(scrollViewer.Offset.X, to)),
                    },
                },
            },
        };

        try
        {
            await animation.RunAsync(scrollViewer, token);
        }
        catch (OperationCanceledException)
        {
            // 动画被打断，忽略
        }
    }

    private void OnScrollViewerSizeChanged(object? sender, SizeChangedEventArgs sizeChangedEventArgs)
    {
        if (sizeChangedEventArgs.HeightChanged)
        {
            UpdateLastItemMinHeight();
        }
    }

    private void UpdateLastItemMinHeight()
    {
        if (_scrollViewer == null || Items.Count == 0) return;

        // 获取最后一个 AnchorItem
        if (Items[^1] is not AnchorItem lastItem)
            return;

        // 计算可见区域高度
        double viewportHeight = _scrollViewer.Viewport.Height;

        /*
        // 获取最后一个项目相对于滚动视图的位置
        var point = lastItem.TranslatePoint(new Point(0, 0), _scrollViewer);
        if (!point.HasValue) return;

        // 计算最后一个项目到视口底部的距离
        double remainingHeight = Math.Max(0, viewportHeight - point.Value.Y);
        */

        // 设置最小高度
        lastItem.MinHeight = Math.Max(viewportHeight, lastItem.HeaderHeight);
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        if (_scrollViewer != null)
        {
            _scrollViewer.ScrollChanged -= OnScrollChanged;
            _scrollViewer.SizeChanged -= OnScrollViewerSizeChanged;
        }

        _scrollCts?.Cancel();
        _scrollCts = null;
        UnhookIndexSource();
    }
}
