using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Styling;
using Avalonia.Threading;
using QwQ_Music.Models;

// 添加 DispatcherTimer 的 using

namespace QwQ_Music.UI.Controls;

/// <summary>
///     歌词显示控件
/// </summary>
public class LyricsControl : TemplatedControl
{
    static LyricsControl()
    {
        AffectsRender<LyricsControl>(
            LyricsDataProperty,
            CurrentLyricIndexProperty,
            ShowTranslationProperty,
            LineHeightProperty,
            LineSpacingProperty,
            LyricTextAlignmentProperty,
            TextMarginProperty
        );

        // 监听属性变化
        CurrentLyricIndexProperty.Changed.AddClassHandler<LyricsControl>((x, _) => x.UpdateCurrentLyric());
        LyricsDataProperty.Changed.AddClassHandler<LyricsControl>((x, _) => x.InitializeLyrics());

        // 合并渲染相关属性的处理
        var renderProperties = new AvaloniaProperty[]
        {
            ShowTranslationProperty, LyricTextAlignmentProperty, LineHeightProperty, LineSpacingProperty, TextMarginProperty,
        };

        foreach (var property in renderProperties)
        {
            property.Changed.AddClassHandler<LyricsControl>((x, _) => x.RenderLyrics());
        }

        // 监听 Bounds 变化以触发防抖渲染
        BoundsProperty.Changed.AddClassHandler<LyricsControl>((x, e) => x.OnBoundsChanged(e));
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        // 解除之前的事件绑定
        UnsubscribeEvents();

        // 获取控件模板中的元素
        _scrollViewer = e.NameScope.Find<ScrollViewer>("PART_ScrollViewer");
        _lyricsCanvas = e.NameScope.Find<Canvas>("PART_LyricsCanvas");

        // 初始化防抖计时器
        InitializeDebounceTimer();

        // 绑定事件
        SubscribeEvents();

        // 初始化歌词
        InitializeLyrics();
    }

    private void InitializeDebounceTimer()
    {
        _renderDebounceTimer = new DispatcherTimer
        {
            Interval = _renderDebounceInterval,
        };

        _renderDebounceTimer.Tick += (_, _) =>
        {
            _renderDebounceTimer?.Stop();
            RenderLyrics();
        };
    }

    private void UnsubscribeEvents()
    {
        if (_scrollViewer != null)
        {
            _scrollViewer.ScrollChanged -= ScrollViewer_ScrollChanged;
        }

        if (_lyricsCanvas != null)
        {
            _lyricsCanvas.PointerPressed -= LyricsCanvas_PointerPressed;
        }

        // 停止并清理计时器
        _renderDebounceTimer?.Stop();
        _renderDebounceTimer = null;
    }

    private void SubscribeEvents()
    {
        if (_scrollViewer != null)
        {
            _scrollViewer.ScrollChanged += ScrollViewer_ScrollChanged;
        }

        if (_lyricsCanvas != null)
        {
            _lyricsCanvas.PointerPressed += LyricsCanvas_PointerPressed;
        }
    }

    /// <summary>
    ///     处理滚动事件
    /// </summary>
    private void ScrollViewer_ScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        // 忽略由于内容或视口大小变化引起的滚动事件
        if (e.ExtentDelta != Vector.Zero || e.ViewportDelta != Vector.Zero || _isProgrammaticScrolling)
            return;

        // 检测用户滚动
        if (!(Math.Abs(e.OffsetDelta.Y) > 0.1))
            return;

        _isUserScrolling = true;
        _lastUserScrollTime = DateTime.Now;
    }

    /// <summary>
    ///     处理歌词点击事件
    /// </summary>
    private void LyricsCanvas_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_lyricsCanvas == null || _lyricLines.Count == 0)
            return;

        var position = e.GetPosition(_lyricsCanvas);

        // 查找点击的歌词行
        foreach (var line in _lyricLines.Where(line => line.Bounds.Contains(position)))
        {
            ClickedLyricTime = line.TimePoint;
            ClickedLyricText = line.Text;

            // 触发歌词点击事件
            LyricClicked?.Invoke(this, line.TimePoint, line.Text);

            break;
        }
    }

    /// <summary>
    ///     处理控件边界变化事件，用于防抖渲染
    /// </summary>
    private void OnBoundsChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (
            e is not { OldValue: Rect oldBounds, NewValue: Rect newBounds }
         || Math.Abs(oldBounds.Height - newBounds.Height) <= 0.1
         && Math.Abs(oldBounds.Width - newBounds.Width) <= 0.1
        )
            return;

        // 重置并启动防抖计时器
        _renderDebounceTimer?.Stop();
        _renderDebounceTimer?.Start();
    }

    /// <summary>
    ///     初始化歌词数据
    /// </summary>
    private void InitializeLyrics()
    {
        if (_lyricsCanvas == null)
            return;

        // 清空之前的数据
        ClearLyricResources();

        // 检查LyricsData是否有效
        if (LyricsData.Lyrics.Count == 0)
            return;

        // 设置歌词数据
        _lyrics = LyricsData.Lyrics;

        // 渲染歌词
        RenderLyrics();

        // 更新当前歌词
        UpdateCurrentLyric();
    }

    /// <summary>
    ///     渲染歌词
    /// </summary>
    private void RenderLyrics()
    {
        // 停止可能正在运行的防抖计时器，防止重复渲染
        _renderDebounceTimer?.Stop();

        if (_lyricsCanvas == null || _lyrics == null || _lyrics.Count == 0 || _lyricsCanvas.Bounds.Width <= 0) // 检查宽度是否大于0
            return;

        _lyricsCanvas.Children.Clear();
        _lyricLines.Clear();

        double yPosition = 0;
        double availableWidth = _lyricsCanvas.Bounds.Width;
        bool autoHeight = LineHeight <= 0;

        // 渲染每一行歌词
        foreach ((double timePoint, string primaryText, string? translation) in _lyrics)
        {
            string? translationText = ShowTranslation ? translation : null;

            // 创建歌词行控件
            var lyricLineControl = new LyricLineControl
            {
                Text = primaryText,
                Translation = translationText,
                TimePoint = timePoint,
                ShowTranslation = ShowTranslation && !string.IsNullOrEmpty(translationText),
                TextAlignment = LyricTextAlignment,
                Width = availableWidth,
                TextMargin = TextMargin,
                TranslationSpacing = TranslationSpacing,
            };

            if (string.IsNullOrWhiteSpace(primaryText))
            {
                lyricLineControl.Classes.Add("empty");
            }

            _lyricsCanvas.Children.Add(lyricLineControl);

            // 测量控件实际高度
            lyricLineControl.Measure(new Size(availableWidth, double.PositiveInfinity));
            double lineHeight = autoHeight ? lyricLineControl.DesiredSize.Height : LineHeight;

            // 设置位置
            Canvas.SetLeft(lyricLineControl, 0);
            Canvas.SetTop(lyricLineControl, yPosition);

            // 记录歌词行信息
            _lyricLines.Add(lyricLineControl);

            // 更新下一行的位置
            yPosition += lineHeight + LineSpacing;
        }

        // 确保内容高度至少等于控件高度，以便滚动正常工作
        _lyricsCanvas.Height = Math.Max(yPosition, _scrollViewer?.Bounds.Height ?? 0);

        // 更新当前歌词样式
        if (CurrentLyricIndex >= 0 && CurrentLyricIndex < _lyricLines.Count)
        {
            UpdateHighlight(CurrentLyricIndex);
        }

        // 滚动到当前歌词
        ScrollToCurrentLyric(); // 初始渲染时不使用动画
    }

    /// <summary>
    ///     更新当前歌词高亮和滚动
    /// </summary>
    private void UpdateCurrentLyric()
    {
        if (
            _lyricsCanvas == null
         || _lyricLines.Count == 0
         || CurrentLyricIndex < 0
         || CurrentLyricIndex >= _lyricLines.Count
        )
            return;

        // 更新高亮
        UpdateHighlight(CurrentLyricIndex);

        // 滚动到当前歌词
        ScrollToCurrentLyric();
    }

    /// <summary>
    ///     更新歌词高亮样式
    /// </summary>
    /// <param name="newIndex">新的高亮索引</param>
    private void UpdateHighlight(int newIndex)
    {
        // 移除旧的高亮
        if (_lastHighlightedIndex >= 0 && _lastHighlightedIndex < _lyricLines.Count)
        {
            _lyricLines[_lastHighlightedIndex].Classes.Remove("current");
        }

        // 添加新的高亮
        if (newIndex >= 0 && newIndex < _lyricLines.Count)
        {
            _lyricLines[newIndex].Classes.Add("current");
            _lastHighlightedIndex = newIndex;
        }
        else
        {
            _lastHighlightedIndex = -1;
        }
    }

    /// <summary>
    ///     滚动到当前歌词行
    /// </summary>
    private void ScrollToCurrentLyric()
    {
        if (
            _scrollViewer == null
         || _lyricsCanvas == null
         || CurrentLyricIndex < 0
         || CurrentLyricIndex >= _lyricLines.Count
        )
            return;

        // 如果用户正在滚动，则不进行程序滚动
        if (_isUserScrolling && DateTime.Now - _lastUserScrollTime < _userScrollTimeout)
            return;

        _isUserScrolling = false; // 重置用户滚动标志

        var currentLine = _lyricLines[CurrentLyricIndex];
        double targetOffset = CalculateScrollOffset(currentLine);

        // 取消之前的动画
        _scrollAnimationCts?.Cancel();
        _scrollAnimationCts = new CancellationTokenSource();
        var cancellationToken = _scrollAnimationCts.Token;

        _isProgrammaticScrolling = true; // 标记为程序滚动

        var animation = new Animation
        {
            Duration = ScrollAnimationDuration,
            Easing = ScrollEasing,
            FillMode = FillMode.Forward,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0d),
                    Setters =
                    {
                        new Setter
                        {
                            Property = ScrollViewer.OffsetProperty,
                            Value = _scrollViewer.Offset,
                        },
                    },
                },
                new KeyFrame
                {
                    Cue = new Cue(1d),
                    Setters =
                    {
                        new Setter
                        {
                            Property = ScrollViewer.OffsetProperty,
                            Value = new Vector(_scrollViewer.Offset.X, targetOffset),
                        },
                    },
                },
            },
        };

        // 使用 RunAsync 并捕获 TaskCanceledException
        animation
            .RunAsync(_scrollViewer, cancellationToken)
            .ContinueWith(
                task =>
                {
                    // 动画完成或取消后，重置程序滚动标志
                    Dispatcher.UIThread.Post(() =>
                    {
                        // 只有当这是最后一个动画时才重置标志
                        if (cancellationToken.IsCancellationRequested)
                            return;

                        _isProgrammaticScrolling = false;
                        _scrollAnimationCts = null;
                    });

                    if (task.IsCanceled)
                    {
                        // LoggerService.Debug("滚动动画被取消");
                    }
                    else if (task.IsFaulted)
                    {
                        Debug.WriteLine($"滚动动画出错: {task.Exception}");
                    }
                },
                TaskScheduler.FromCurrentSynchronizationContext()
            );
    }

    /// <summary>
    ///     计算滚动偏移量，使当前行居中显示
    /// </summary>
    /// <param name="currentLine">当前歌词行控件</param>
    /// <returns>目标滚动偏移量</returns>
    private double CalculateScrollOffset(LyricLineControl currentLine)
    {
        if (_scrollViewer == null)
            return 0;

        double viewportHeight = _scrollViewer.Bounds.Height;
        double centerY = viewportHeight / 2;
        double lyricY = Canvas.GetTop(currentLine);
        double lyricHeight = currentLine.DesiredSize.Height;
        double lyricCenterY = lyricY + lyricHeight / 2;
        double targetOffset;

        if (lyricCenterY <= centerY)
        {
            // 如果歌词位置未超过控件中心线，但总高度超过视口，则保持在顶部
            targetOffset = 0;
        }
        else
        {
            // 歌词位置超过中心线，需要将歌词滚动到中心
            targetOffset = lyricY - (viewportHeight - lyricHeight) / 2;
        }

        return targetOffset;
    }

    /// <summary>
    ///     清理歌词相关资源
    /// </summary>
    private void ClearLyricResources()
    {
        _lyricsCanvas?.Children.Clear();
        _lyricLines.Clear();
        _lyrics = null;
        _lastHighlightedIndex = -1;
        _scrollAnimationCts?.Cancel(); // 取消可能正在进行的动画
        _scrollAnimationCts = null;
        _isUserScrolling = false; // 重置用户滚动状态
        _isProgrammaticScrolling = false; // 重置程序滚动状态

        // 重置滚动条位置
        if (_scrollViewer != null)
        {
            _scrollViewer.Offset = Vector.Zero;
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        // 清理资源
        UnsubscribeEvents();
        ClearLyricResources();
    }

    #region 依赖属性

    // 歌词数据
    public static readonly StyledProperty<LyricsData> LyricsDataProperty = AvaloniaProperty.Register<
        LyricsControl,
        LyricsData
    >(nameof(LyricsData));

    // 当前歌词索引
    public static readonly StyledProperty<int> CurrentLyricIndexProperty = AvaloniaProperty.Register<
        LyricsControl,
        int
    >(nameof(CurrentLyricIndex), -1, defaultBindingMode: BindingMode.OneWay);

    // 是否显示翻译
    public static readonly StyledProperty<bool> ShowTranslationProperty = AvaloniaProperty.Register<
        LyricsControl,
        bool
    >(nameof(ShowTranslation), true);

    // 歌词行高
    public static readonly StyledProperty<double> LineHeightProperty = AvaloniaProperty.Register<LyricsControl, double>(
        nameof(LineHeight) // 修改默认值为0，表示自动计算
    );

    // 歌词行间距
    public static readonly StyledProperty<double> LineSpacingProperty = AvaloniaProperty.Register<
        LyricsControl,
        double
    >(nameof(LineSpacing));

    // 滚动动画持续时间
    public static readonly StyledProperty<TimeSpan> ScrollAnimationDurationProperty = AvaloniaProperty.Register<
        LyricsControl,
        TimeSpan
    >(nameof(ScrollAnimationDuration), TimeSpan.FromMilliseconds(500));

    // 滚动缓动函数
    public static readonly StyledProperty<Easing> ScrollEasingProperty = AvaloniaProperty.Register<
        LyricsControl,
        Easing
    >(nameof(ScrollEasing), new CubicEaseOut());

    // 歌词文本对齐方式
    public static readonly StyledProperty<HorizontalAlignment> LyricTextAlignmentProperty = AvaloniaProperty.Register<
        LyricsControl,
        HorizontalAlignment
    >(nameof(LyricTextAlignment), HorizontalAlignment.Center);

    // 歌词文本边距
    public static readonly StyledProperty<Thickness> TextMarginProperty = AvaloniaProperty.Register<
        LyricsControl,
        Thickness
    >(nameof(TextMargin), new Thickness(20, 10));

    // 翻译文本间距
    public static readonly StyledProperty<double> TranslationSpacingProperty = AvaloniaProperty.Register<
        LyricsControl,
        double
    >(nameof(TranslationSpacing));

    // 点击的歌词时间点
    public static readonly StyledProperty<double> ClickedLyricTimeProperty = AvaloniaProperty.Register<
        LyricsControl,
        double
    >(nameof(ClickedLyricTime), defaultBindingMode: BindingMode.OneWayToSource);

    // 点击的歌词文本
    public static readonly StyledProperty<string> ClickedLyricTextProperty = AvaloniaProperty.Register<
        LyricsControl,
        string
    >(nameof(ClickedLyricText), string.Empty, defaultBindingMode: BindingMode.OneWayToSource);

    #endregion

    #region 事件

    /// <summary>
    ///     歌词点击事件委托
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="timePoint">点击的歌词时间点</param>
    /// <param name="text">点击的歌词文本</param>
    public delegate void LyricClickedEventHandler(object sender, double timePoint, string text);

    /// <summary>
    ///     歌词点击事件
    /// </summary>
    public event LyricClickedEventHandler? LyricClicked;

    #endregion

    #region 属性

    public LyricsData LyricsData
    {
        get => GetValue(LyricsDataProperty);
        set => SetValue(LyricsDataProperty, value);
    }

    public int CurrentLyricIndex
    {
        get => GetValue(CurrentLyricIndexProperty);
        set => SetValue(CurrentLyricIndexProperty, value);
    }

    public bool ShowTranslation
    {
        get => GetValue(ShowTranslationProperty);
        set => SetValue(ShowTranslationProperty, value);
    }

    public double LineHeight
    {
        get => GetValue(LineHeightProperty);
        set => SetValue(LineHeightProperty, value);
    }

    public double LineSpacing
    {
        get => GetValue(LineSpacingProperty);
        set => SetValue(LineSpacingProperty, value);
    }

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

    public HorizontalAlignment LyricTextAlignment
    {
        get => GetValue(LyricTextAlignmentProperty);
        set => SetValue(LyricTextAlignmentProperty, value);
    }

    public Thickness TextMargin
    {
        get => GetValue(TextMarginProperty);
        set => SetValue(TextMarginProperty, value);
    }

    public double TranslationSpacing
    {
        get => GetValue(TranslationSpacingProperty);
        set => SetValue(TranslationSpacingProperty, value);
    }

    public double ClickedLyricTime
    {
        get => GetValue(ClickedLyricTimeProperty);
        private set => SetValue(ClickedLyricTimeProperty, value);
    }

    public string ClickedLyricText
    {
        get => GetValue(ClickedLyricTextProperty);
        private set => SetValue(ClickedLyricTextProperty, value);
    }

    #endregion

    #region 私有字段

    private Canvas? _lyricsCanvas;
    private ScrollViewer? _scrollViewer;
    private readonly List<LyricLineControl> _lyricLines = [];
    private bool _isUserScrolling;
    private DateTime _lastUserScrollTime = DateTime.MinValue;
    private readonly TimeSpan _userScrollTimeout = TimeSpan.FromSeconds(3);
    private List<LyricLine>? _lyrics;
    private bool _isProgrammaticScrolling;

    // 跟踪当前高亮的歌词索引
    private int _lastHighlightedIndex = -1;

    // 添加动画取消令牌源
    private CancellationTokenSource? _scrollAnimationCts;

    // 添加用于防抖渲染的计时器
    private DispatcherTimer? _renderDebounceTimer;
    private readonly TimeSpan _renderDebounceInterval = TimeSpan.FromMilliseconds(150); // 防抖间隔

    #endregion
}
