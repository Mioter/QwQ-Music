using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using QwQ_Music.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Avalonia.Data;

namespace QwQ_Music.Controls;

/// <summary>
/// 歌词显示控件
/// </summary>
public class LyricsControl : TemplatedControl
{
    #region 依赖属性

    // 歌词数据
    public static readonly StyledProperty<LyricsData> LyricsDataProperty =
        AvaloniaProperty.Register<LyricsControl, LyricsData>(nameof(LyricsData));

    // 当前播放时间（秒）
    public static readonly StyledProperty<double> CurrentTimeProperty =
        AvaloniaProperty.Register<LyricsControl, double>(nameof(CurrentTime),defaultBindingMode: BindingMode.OneWay);

    // 是否显示翻译
    public static readonly StyledProperty<bool> ShowTranslationProperty =
        AvaloniaProperty.Register<LyricsControl, bool>(nameof(ShowTranslation), true);

    // 当前选中的歌词索引
    public static readonly StyledProperty<int> CurrentLyricIndexProperty =
        AvaloniaProperty.Register<LyricsControl, int>(nameof(CurrentLyricIndex), -1);

    // 歌词行高
    public static readonly StyledProperty<double> LineHeightProperty =
        AvaloniaProperty.Register<LyricsControl, double>(nameof(LineHeight), 40.0);

    // 歌词行间距
    public static readonly StyledProperty<double> LineSpacingProperty =
        AvaloniaProperty.Register<LyricsControl, double>(nameof(LineSpacing), 10.0);

    // 当前歌词字体大小
    public static readonly StyledProperty<double> CurrentLyricFontSizeProperty =
        AvaloniaProperty.Register<LyricsControl, double>(nameof(CurrentLyricFontSize), 18.0);

    // 普通歌词字体大小
    public static readonly StyledProperty<double> NormalLyricFontSizeProperty =
        AvaloniaProperty.Register<LyricsControl, double>(nameof(NormalLyricFontSize), 16.0);

    // 翻译歌词字体大小
    public static readonly StyledProperty<double> TranslationFontSizeProperty =
        AvaloniaProperty.Register<LyricsControl, double>(nameof(TranslationFontSize), 14.0);

    // 当前歌词前景色
    public static readonly StyledProperty<IBrush> CurrentLyricForegroundProperty =
        AvaloniaProperty.Register<LyricsControl, IBrush>(nameof(CurrentLyricForeground), 
            new ImmutableSolidColorBrush(Colors.White));

    // 普通歌词前景色
    public static readonly StyledProperty<IBrush> NormalLyricForegroundProperty =
        AvaloniaProperty.Register<LyricsControl, IBrush>(nameof(NormalLyricForeground), 
            new ImmutableSolidColorBrush(Color.FromRgb(200, 200, 200)));

    // 翻译歌词前景色
    public static readonly StyledProperty<IBrush> TranslationForegroundProperty =
        AvaloniaProperty.Register<LyricsControl, IBrush>(nameof(TranslationForeground), 
            new ImmutableSolidColorBrush(Color.FromRgb(180, 180, 180)));

    // 滚动动画持续时间
    public static readonly StyledProperty<TimeSpan> ScrollAnimationDurationProperty =
        AvaloniaProperty.Register<LyricsControl, TimeSpan>(nameof(ScrollAnimationDuration), 
            TimeSpan.FromMilliseconds(500));

    // 滚动缓动函数
    public static readonly StyledProperty<Easing> ScrollEasingProperty =
        AvaloniaProperty.Register<LyricsControl, Easing>(nameof(ScrollEasing), 
            new CubicEaseOut());
    
    // 歌词文本对齐方式
    public static readonly StyledProperty<TextAlignment> LyricTextAlignmentProperty =
        AvaloniaProperty.Register<LyricsControl, TextAlignment>(nameof(LyricTextAlignment), 
            TextAlignment.Center);
    
    // 歌词文本过渡动画
    public static readonly StyledProperty<Transitions?> LyricTransitionsProperty =
        AvaloniaProperty.Register<LyricsControl, Transitions?>(nameof(LyricTransitions));
    
    // 歌词文本特效
    public static readonly StyledProperty<IEffect?> LyricEffectProperty =
        AvaloniaProperty.Register<LyricsControl, IEffect?>(nameof(LyricEffect));
    
    // 点击的歌词时间点
    public static readonly StyledProperty<double> ClickedLyricTimeProperty =
        AvaloniaProperty.Register<LyricsControl, double>(nameof(ClickedLyricTime), 
            defaultBindingMode: BindingMode.OneWayToSource);
    
    // 点击的歌词文本
    public static readonly StyledProperty<string> ClickedLyricTextProperty =
        AvaloniaProperty.Register<LyricsControl, string>(nameof(ClickedLyricText), 
            string.Empty, defaultBindingMode: BindingMode.OneWayToSource);
    
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

    public double CurrentTime
    {
        get => GetValue(CurrentTimeProperty);
        set => SetValue(CurrentTimeProperty, value);
    }

    public bool ShowTranslation
    {
        get => GetValue(ShowTranslationProperty);
        set => SetValue(ShowTranslationProperty, value);
    }

    public int CurrentLyricIndex
    {
        get => GetValue(CurrentLyricIndexProperty);
        set => SetValue(CurrentLyricIndexProperty, value);
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

    public double CurrentLyricFontSize
    {
        get => GetValue(CurrentLyricFontSizeProperty);
        set => SetValue(CurrentLyricFontSizeProperty, value);
    }

    public double NormalLyricFontSize
    {
        get => GetValue(NormalLyricFontSizeProperty);
        set => SetValue(NormalLyricFontSizeProperty, value);
    }

    public double TranslationFontSize
    {
        get => GetValue(TranslationFontSizeProperty);
        set => SetValue(TranslationFontSizeProperty, value);
    }

    public IBrush CurrentLyricForeground
    {
        get => GetValue(CurrentLyricForegroundProperty);
        set => SetValue(CurrentLyricForegroundProperty, value);
    }

    public IBrush NormalLyricForeground
    {
        get => GetValue(NormalLyricForegroundProperty);
        set => SetValue(NormalLyricForegroundProperty, value);
    }

    public IBrush TranslationForeground
    {
        get => GetValue(TranslationForegroundProperty);
        set => SetValue(TranslationForegroundProperty, value);
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
    
    public TextAlignment LyricTextAlignment
    {
        get => GetValue(LyricTextAlignmentProperty);
        set => SetValue(LyricTextAlignmentProperty, value);
    }
    
    public Transitions? LyricTransitions
    {
        get => GetValue(LyricTransitionsProperty);
        set => SetValue(LyricTransitionsProperty, value);
    }
    
    public IEffect? LyricEffect
    {
        get => GetValue(LyricEffectProperty);
        set => SetValue(LyricEffectProperty, value);
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
    private readonly List<LyricLine> _lyricLines = [];
    private bool _isUserScrolling;
    private DateTime _lastUserScrollTime = DateTime.MinValue;
    private readonly TimeSpan _userScrollTimeout = TimeSpan.FromSeconds(3);
    private Dictionary<double, string>? _primaryLyrics;
    private Dictionary<double, string>? _translationLyrics;
    private List<double> _timePoints = [];
    private bool _isProgrammaticScrolling;
    // 添加动画取消令牌源
    private CancellationTokenSource? _scrollAnimationCts;

    // 空白歌词的占位符
    private const string EmptyLyricPlaceholder = "● ● ●";

    #endregion

    static LyricsControl()
    {
        AffectsRender<LyricsControl>(
            LyricsDataProperty,
            CurrentTimeProperty,
            ShowTranslationProperty,
            CurrentLyricIndexProperty,
            LineHeightProperty,
            LineSpacingProperty, 
            LyricTextAlignmentProperty,
            LyricTransitionsProperty,
            LyricEffectProperty,
            CurrentLyricFontSizeProperty,
            NormalLyricFontSizeProperty,
            TranslationFontSizeProperty,
            CurrentLyricForegroundProperty,
            NormalLyricForegroundProperty,
            TranslationForegroundProperty
        );

        // 监听属性变化
        CurrentTimeProperty.Changed.AddClassHandler<LyricsControl>((x, _) => x.UpdateCurrentLyric());
        LyricsDataProperty.Changed.AddClassHandler<LyricsControl>((x, _) => x.InitializeLyrics());
        
        // 合并渲染相关属性的处理
        var renderProperties = new AvaloniaProperty[]
        {
            ShowTranslationProperty,
            LyricTextAlignmentProperty,
            LyricTransitionsProperty,
            LyricEffectProperty,
            LineHeightProperty,
            LineSpacingProperty
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
        if (LyricsData.PrimaryLyrics.Count == 0)
            return;

        // 设置歌词数据
        _primaryLyrics = LyricsData.PrimaryLyrics;
        _translationLyrics = LyricsData.TranslationLyrics;
            
        // 获取所有时间点并排序
        _timePoints = _primaryLyrics.Keys.OrderBy(k => k).ToList();
            
        // 渲染歌词并更新当前歌词
        RenderLyrics();
        UpdateCurrentLyric();
    }

    /// <summary>
    /// 渲染歌词
    /// </summary>
    private void RenderLyrics()
    {
        if (_lyricsCanvas == null || _primaryLyrics == null || _timePoints.Count == 0)
            return;

        _lyricsCanvas.Children.Clear();
        _lyricLines.Clear();

        double yPosition = 0;
        double canvasWidth = GetCanvasWidth();
        
        // 渲染每一行歌词
        foreach (double timePoint in _timePoints)
        {
            string primaryText = _primaryLyrics[timePoint];
            string? translationText = null;
            
            if (ShowTranslation && _translationLyrics != null)
            {
                _translationLyrics.TryGetValue(timePoint, out translationText);
            }

            // 创建并添加主歌词文本块
            var primaryTextBlock = CreateTextBlock(
                primaryText, 
                NormalLyricFontSize, 
                NormalLyricForeground, 
                canvasWidth, 
                yPosition);
                
            _lyricsCanvas.Children.Add(primaryTextBlock);

            // 记录歌词行信息
            var lyricLine = new LyricLine
            {
                TimePoint = timePoint,
                Text = primaryText,
                TextBlock = primaryTextBlock,
                TranslationTextBlock = null,
                Bounds = new Rect(0, yPosition, canvasWidth, LineHeight),
            };
                
            _lyricLines.Add(lyricLine);

            yPosition += LineHeight;

            // 如果有翻译，添加翻译文本块
            if (ShowTranslation && !string.IsNullOrEmpty(translationText))
            {
                var translationTextBlock = CreateTextBlock(
                    translationText, 
                    TranslationFontSize, 
                    TranslationForeground, 
                    canvasWidth, 
                    yPosition);
                    
                _lyricsCanvas.Children.Add(translationTextBlock);
                    
                lyricLine.TranslationTextBlock = translationTextBlock;
                lyricLine.Bounds = new Rect(0, lyricLine.Bounds.Y, canvasWidth, 
                    LineHeight * 2 + LineSpacing);

                yPosition += LineHeight + LineSpacing;
            }

            yPosition += LineSpacing;
        }

        // 确保内容高度至少等于控件高度，以便滚动正常工作
        _lyricsCanvas.Height = Math.Max(yPosition, _scrollViewer?.Bounds.Height ?? 0);
        
        // 更新当前歌词样式
        if (CurrentLyricIndex >= 0 && CurrentLyricIndex < _lyricLines.Count)
        {
            UpdateLyricStyles(-1, CurrentLyricIndex);
        }
    }
    
    /// <summary>
    /// 获取画布宽度
    /// </summary>
    private double GetCanvasWidth()
    {
        double canvasWidth = _lyricsCanvas?.Bounds.Width ?? 0;
        return canvasWidth > 0 ? canvasWidth : _scrollViewer?.Bounds.Width ?? 300;
    }
    
    /// <summary>
    /// 创建文本块
    /// </summary>
    private TextBlock CreateTextBlock(string text, double fontSize, IBrush foreground, double maxWidth, double yPosition)
    {
        var textBlock = new TextBlock
        {
            Text = text,
            FontSize = fontSize,
            TextWrapping = TextWrapping.Wrap,
            Foreground = foreground,
            TextAlignment = LyricTextAlignment,
            MaxWidth = maxWidth,
            Effect = LyricEffect,
            Transitions = LyricTransitions,
        };

        Canvas.SetLeft(textBlock, 0);
        Canvas.SetTop(textBlock, yPosition);
        
        return textBlock;
    }
    
    /// <summary>
    /// 更新当前歌词
    /// </summary>
    private void UpdateCurrentLyric()
    {
        if (_primaryLyrics == null || _timePoints.Count == 0)
            return;

        // 检查用户滚动超时
        if (_isUserScrolling && DateTime.Now - _lastUserScrollTime > _userScrollTimeout)
        {
            _isUserScrolling = false;
        }

        // 查找当前时间对应的歌词索引
        int index = FindCurrentLyricIndex();

        // 如果索引没有变化，不需要更新
        if (index == CurrentLyricIndex) 
            return;
            
        int previousIndex = CurrentLyricIndex;
        CurrentLyricIndex = index;

        // 更新歌词样式
        UpdateLyricStyles(previousIndex, index);

        // 如果不是用户滚动，则滚动到当前歌词
        if (!_isUserScrolling && index >= 0 && index < _lyricLines.Count)
        {
            ScrollToLyric(index);
        }
    }

    /// <summary>
    /// 更新歌词样式
    /// </summary>
    private void UpdateLyricStyles(int previousIndex, int currentIndex)
    {
        // 重置之前的当前歌词样式
        if (previousIndex >= 0 && previousIndex < _lyricLines.Count)
        {
            var previousLine = _lyricLines[previousIndex];
            if (previousLine.TextBlock != null)
            {
                // 如果之前是空白行且是当前行，恢复原始文本
                if (string.IsNullOrWhiteSpace(previousLine.Text) && 
                    previousLine.TextBlock.Text == EmptyLyricPlaceholder)
                {
                    previousLine.TextBlock.Text = previousLine.Text;
                }
                
                // 恢复默认样式
                previousLine.TextBlock.FontSize = NormalLyricFontSize;
                previousLine.TextBlock.Foreground = NormalLyricForeground;
            }
        }

        // 设置当前歌词样式
        if (currentIndex < 0 || currentIndex >= _lyricLines.Count) 
            return;
        
        var currentLine = _lyricLines[currentIndex];
        if (currentLine.TextBlock == null) 
            return;
        
        // 如果当前行是空白行，替换为占位符
        if (string.IsNullOrWhiteSpace(currentLine.Text))
        {
            currentLine.TextBlock.Text = EmptyLyricPlaceholder;
        }
                
        // 应用高亮样式
        currentLine.TextBlock.FontSize = CurrentLyricFontSize;
        currentLine.TextBlock.Foreground = CurrentLyricForeground;
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
                
            animation.Children.Add(new KeyFrame
            {
                Cue = new Cue(0d),
                Setters = { new Setter(ScrollViewer.OffsetProperty, new Vector(_scrollViewer.Offset.X, currentOffset)) },
            });
                
            animation.Children.Add(new KeyFrame
            {
                Cue = new Cue(1d),
                Setters = { new Setter(ScrollViewer.OffsetProperty, new Vector(_scrollViewer.Offset.X, targetOffset)) },
            });
            
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
    private double CalculateScrollTargetOffset(LyricLine lyricLine, double viewportHeight)
    {
        double centerY = viewportHeight / 2;
        double lyricCenterY = lyricLine.Bounds.Y + lyricLine.Bounds.Height / 2;
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
            targetOffset = lyricLine.Bounds.Y - (viewportHeight - lyricLine.Bounds.Height) / 2;
        }
        
        // 确保目标偏移量在有效范围内
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

    // 歌词行信息类
    private class LyricLine
    {
        public double TimePoint { get; init; }
        public string Text { get; init; } = string.Empty;
        public TextBlock? TextBlock { get; init; }
        public TextBlock? TranslationTextBlock { get; set; }
        public Rect Bounds { get; set; }
    }
    
    #region 辅助方法
    
    /// <summary>
    /// 查找当前时间对应的歌词索引
    /// </summary>
    private int FindCurrentLyricIndex()
    {
        if (_timePoints.Count == 0)
            return -1;
            
        // 如果当前时间小于第一个时间点，返回-1
        if (CurrentTime < _timePoints[0])
            return -1;
            
        // 如果当前时间大于等于最后一个时间点，返回最后一个索引
        if (CurrentTime >= _timePoints[^1])
            return _timePoints.Count - 1;
            
        // 二分查找
        int left = 0;
        int right = _timePoints.Count - 1;
        
        while (left <= right)
        {
            int mid = left + (right - left) / 2;
            
            if (mid == _timePoints.Count - 1 || 
                CurrentTime >= _timePoints[mid] && CurrentTime < _timePoints[mid + 1])
                return mid;
                
            if (CurrentTime < _timePoints[mid])
                right = mid - 1;
            else
                left = mid + 1;
        }
        
        return -1;
    }
    
    /// <summary>
    /// 清除所有歌词资源
    /// </summary>
    private void ClearLyricResources()
    {
        _lyricsCanvas?.Children.Clear();

        _lyricLines.Clear();
        _primaryLyrics = null;
        _translationLyrics = null;
        _timePoints.Clear();
        CurrentLyricIndex = -1;
    }
    
    #endregion
}