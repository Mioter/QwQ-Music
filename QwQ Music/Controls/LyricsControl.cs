using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Styling;
using QwQ_Music.Models;

namespace QwQ_Music.Controls;

/// <summary>
/// 歌词显示控件
/// </summary>
public class LyricsControl : TemplatedControl
{
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
    >(nameof(TextMargin), new Thickness(20,10));

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
    /// 歌词点击事件委托
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="timePoint">点击的歌词时间点</param>
    /// <param name="text">点击的歌词文本</param>
    public delegate void LyricClickedEventHandler(object sender, double timePoint, string text);

    /// <summary>
    /// 歌词点击事件
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

    #endregion

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
            ShowTranslationProperty,
            LyricTextAlignmentProperty,
            LineHeightProperty,
            LineSpacingProperty,
            TextMarginProperty,
        };

        foreach (var property in renderProperties)
        {
            property.Changed.AddClassHandler<LyricsControl>((x, _) => x.RenderLyrics());
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        // 解除之前的事件绑定
        UnsubscribeEvents();

        // 获取控件模板中的元素
        _scrollViewer = e.NameScope.Find<ScrollViewer>("PART_ScrollViewer");
        _lyricsCanvas = e.NameScope.Find<Canvas>("PART_LyricsCanvas");

        // 绑定事件
        SubscribeEvents();

        // 初始化歌词
        InitializeLyrics();
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
    /// 处理滚动事件
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
    /// 处理歌词点击事件
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
    /// 初始化歌词数据
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
    /// 渲染歌词
    /// </summary>
    private void RenderLyrics()
    {
        if (_lyricsCanvas == null || _lyrics == null || _lyrics.Count == 0 || _lyricsCanvas.Bounds.Width == 0)
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
            UpdateLyricStyles(CurrentLyricIndex);
        }
    }

    /// <summary>
    /// 更新当前歌词
    /// </summary>
    private void UpdateCurrentLyric()
    {
        if (_lyrics == null)
            return;

        // 检查用户滚动超时
        if (_isUserScrolling && DateTime.Now - _lastUserScrollTime > _userScrollTimeout)
        {
            _isUserScrolling = false;
        }

        // 确保索引在有效范围内
        int index = Math.Clamp(CurrentLyricIndex, -1, _lyricLines.Count - 1);

        // 更新歌词样式
        UpdateLyricStyles(index);

        // 如果不是用户滚动，则滚动到当前歌词
        if (!_isUserScrolling && index >= 0 && index < _lyricLines.Count)
        {
            ScrollToLyric(index);
        }
    }

    /// <summary>
    /// 更新歌词样式
    /// </summary>
    private void UpdateLyricStyles(int currentIndex)
    {
        // 如果索引无效，直接返回
        if (currentIndex < -1 || currentIndex >= _lyricLines.Count)
            return;

        // 重置上一个高亮的歌词样式
        if (_lastHighlightedIndex >= 0 && _lastHighlightedIndex < _lyricLines.Count)
        {
            var lastLine = _lyricLines[_lastHighlightedIndex];
            lastLine.Classes.Remove("current");
        }

        // 设置当前歌词样式
        if (currentIndex >= 0)
        {
            var currentLine = _lyricLines[currentIndex];
            currentLine.Classes.Add("current");
        }

        // 更新上一个高亮索引
        _lastHighlightedIndex = currentIndex;
    }

    /// <summary>
    /// 滚动到指定歌词行
    /// </summary>
    private async void ScrollToLyric(int index)
    {
        if (_scrollViewer == null || index < 0 || index >= _lyricLines.Count || _lyricsCanvas == null)
            return;

        // 取消正在进行的滚动动画
        _scrollAnimationCts?.Cancel();
        _scrollAnimationCts = new CancellationTokenSource();
        var cancellationToken = _scrollAnimationCts.Token;

        _isProgrammaticScrolling = true;

        try
        {
            var lyricLine = _lyricLines[index];
            double viewportHeight = _scrollViewer.Bounds.Height;

            // 计算目标偏移量
            double targetOffset = CalculateScrollTargetOffset(lyricLine, viewportHeight);
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
        finally
        {
            // 只有当这是最后一个动画时才重置标志
            if (!cancellationToken.IsCancellationRequested)
            {
                _isProgrammaticScrolling = false;
                _scrollAnimationCts = null;
            }
        }
    }

    /// <summary>
    /// 计算滚动目标偏移量
    /// </summary>
    private double CalculateScrollTargetOffset(LyricLineControl lyricLineControl, double viewportHeight)
    {
        double centerY = viewportHeight / 2;
        double lyricY = Canvas.GetTop(lyricLineControl);
        double lyricHeight = lyricLineControl.DesiredSize.Height;
        double lyricCenterY = lyricY + lyricHeight / 2;
        double targetOffset;

        if (lyricCenterY <= centerY && _lyricsCanvas!.Height <= viewportHeight)
        {
            // 如果歌词位置未超过控件中心线，且所有歌词总高度不超过视口高度，则不滚动
            targetOffset = 0;
        }
        else if (lyricCenterY <= centerY)
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

    protected override Size MeasureOverride(Size availableSize)
    {
        if (_lyricsCanvas != null)
        {
            _lyricsCanvas.Width = availableSize.Width;
        }

        return base.MeasureOverride(availableSize);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (_lyricsCanvas != null && Math.Abs(_lyricsCanvas.Width - finalSize.Width) > 0.1)
        {
            _lyricsCanvas.Width = finalSize.Width;
            RenderLyrics();
        }

        return base.ArrangeOverride(finalSize);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property != BoundsProperty)
            return;

        // 控件大小变化时重新渲染歌词
        RenderLyrics();

        // 如果当前有选中的歌词，重新滚动到该歌词
        if (CurrentLyricIndex >= 0 && CurrentLyricIndex < _lyricLines.Count && !_isUserScrolling)
        {
            ScrollToLyric(CurrentLyricIndex);
        }
    }

    #region 辅助方法

    /// <summary>
    /// 清除所有歌词资源
    /// </summary>
    private void ClearLyricResources()
    {
        _lyricsCanvas?.Children.Clear();

        _lyricLines.Clear();
        _lyrics = null;
    }

    #endregion
}