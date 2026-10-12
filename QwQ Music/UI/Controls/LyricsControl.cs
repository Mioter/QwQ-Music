using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
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
using QwQ_Music.Models;

namespace QwQ_Music.UI.Controls;

/// <summary>
///     歌词显示控件
/// </summary>
public class LyricsControl : TemplatedControl {
    static LyricsControl() {
        CurrentLyricIndexProperty.Changed.AddClassHandler<LyricsControl>((x, _) => x.UpdateCurrentLyric());
        LyricsDataProperty.Changed.AddClassHandler<LyricsControl>((x, _) => x.RebuildLyrics());
        LineHeightProperty.Changed.AddClassHandler<LyricsControl>((x, _) => x.ApplyLineMetrics());
        LineSpacingProperty.Changed.AddClassHandler<LyricsControl>((x, _) => x.ApplyLineMetrics());
        ShowTranslationProperty.Changed.AddClassHandler<LyricsControl>((x, _) => x.ApplyTranslationVisibility());
        BoundsProperty.Changed.AddClassHandler<LyricsControl>((x, _) => x.ScheduleCenterCurrentLyric());
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e) {
        base.OnApplyTemplate(e);

        DetachEvents();

        _lyricsList = e.NameScope.Find<ListBox>("PART_LyricsList");
        _scrollViewer = e.NameScope.Find<ScrollViewer>("PART_ScrollViewer");

        AttachEvents();
        RebuildLyrics();
        Dispatcher.UIThread.Post(TryResolveScrollViewer, DispatcherPriority.Background);
    }

    private void AttachEvents() {
        if (_lyricsList != null)
            _lyricsList.SelectionChanged += LyricsList_SelectionChanged;

        if (_scrollViewer != null)
            _scrollViewer.ScrollChanged += ScrollViewer_ScrollChanged;
    }

    private void DetachEvents() {
        if (_lyricsList != null)
            _lyricsList.SelectionChanged -= LyricsList_SelectionChanged;

        if (_scrollViewer != null)
            _scrollViewer.ScrollChanged -= ScrollViewer_ScrollChanged;
    }

    private void TryResolveScrollViewer() {
        if (_lyricsList == null)
            return;

        _scrollViewer ??= _lyricsList.FindDescendantOfType<ScrollViewer>();

        if (_scrollViewer == null)
            return;

        _scrollViewer.ScrollChanged -= ScrollViewer_ScrollChanged;
        _scrollViewer.ScrollChanged += ScrollViewer_ScrollChanged;
    }

    private void ScrollViewer_ScrollChanged(object? sender, ScrollChangedEventArgs e) {
        if (e.ExtentDelta != Vector.Zero || e.ViewportDelta != Vector.Zero || _isProgrammaticScrolling)
            return;

        if (Math.Abs(e.OffsetDelta.Y) <= 0.1)
            return;

        _isUserScrolling = true;
        _lastUserScrollTime = DateTime.Now;
    }

    private void LyricsList_SelectionChanged(object? sender, SelectionChangedEventArgs e) {
        if (e.AddedItems.Count == 0 || e.AddedItems[0] is not LyricLineViewItem line)
            return;

        ClickedLyricTime = line.TimePoint;
        ClickedLyricText = line.PrimaryText;
        LyricClicked?.Invoke(this, line.TimePoint, line.PrimaryText);

        // 立即清空选择，确保连续点击同一行也能触发跳转。
        Dispatcher.UIThread.Post(() => { _lyricsList?.SelectedIndex = -1; }, DispatcherPriority.Background);
    }

    private void RebuildLyrics() {
        // It can be null at the first call
        if (LyricsData is null)
            return;
        _scrollAnimationCts?.Cancel();
        _scrollAnimationCts = null;
        _isProgrammaticScrolling = false;
        _isUserScrolling = false;

        _lyricItems.Clear();

        foreach (LyricLine line in LyricsData.Data) {
            _lyricItems.Add(
                new LyricLineViewItem(
                    line.TimePoint,
                    line.Primary,
                    line.Secondary,
                    !string.IsNullOrWhiteSpace(line.Secondary),
                    string.IsNullOrWhiteSpace(line.Primary),
                    ResolveLineHeight(),
                    ResolveLineMargin()));
        }

        ApplyTranslationVisibility();

        UpdateCurrentLyric();
    }

    private void ApplyTranslationVisibility() {
        foreach (LyricLineViewItem item in _lyricItems)
            item.ShowTranslation = ShowTranslation && item.HasTranslation;

        ScheduleCenterCurrentLyric();
    }

    private void ApplyLineMetrics() {
        double lineHeight = ResolveLineHeight();
        Thickness margin = ResolveLineMargin();

        foreach (LyricLineViewItem item in _lyricItems) {
            item.LineHeight = lineHeight;
            item.Margin = margin;
        }

        ScheduleCenterCurrentLyric();
    }

    private double ResolveLineHeight() { return LineHeight > 0 ? LineHeight : double.NaN; }

    private Thickness ResolveLineMargin() { return new Thickness(0, 0, 0, Math.Max(0, LineSpacing)); }

    private void UpdateCurrentLyric() {
        if (_lyricItems.Count == 0 || CurrentLyricIndex < 0 || CurrentLyricIndex >= _lyricItems.Count)
            return;

        if (_lastHighlightedIndex >= 0 && _lastHighlightedIndex < _lyricItems.Count)
            _lyricItems[_lastHighlightedIndex].IsCurrent = false;

        _lyricItems[CurrentLyricIndex].IsCurrent = true;
        _lastHighlightedIndex = CurrentLyricIndex;
        ScheduleCenterCurrentLyric();
    }

    private void ScheduleCenterCurrentLyric() {
        if (!IsEffectivelyVisible) {
            LoggerService.Debug("由于控件不可见，跳过本次歌词中置");
            return;
        }

        if (CurrentLyricIndex < 0 || CurrentLyricIndex >= _lyricItems.Count)
            return;

        if (_isUserScrolling && DateTime.Now - _lastUserScrollTime < _userScrollTimeout)
            return;

        _isUserScrolling = false;

        LyricLineViewItem item = _lyricItems[CurrentLyricIndex];
        _lyricsList?.ScrollIntoView(item);

        Dispatcher.UIThread.Post(
            () => ScrollItemToCenterAsync(item).ContinueWith(LoggerService.HandleException),
            DispatcherPriority.Background);
    }

    private async Task ScrollItemToCenterAsync(LyricLineViewItem item) {
        TryResolveScrollViewer();
        if (_lyricsList == null || _scrollViewer == null)
            return;

        Control? container = _lyricsList.ContainerFromItem(item);
        if (container == null)
            return;

        double targetOffset = CalculateTargetOffset(container);
        if (_scrollAnimationCts is not null) {
            // ReSharper disable once MethodHasAsyncOverload
            _scrollAnimationCts.Cancel();
            _scrollAnimationCts.Dispose();
        }

        _scrollAnimationCts = new CancellationTokenSource();
        CancellationToken token = _scrollAnimationCts.Token;
        _isProgrammaticScrolling = true;

        try {
            await AnimateScrollToAsync(targetOffset, token).ConfigureAwait(true);
        } catch (TaskCanceledException) { } finally {
            if (_scrollAnimationCts is { IsCancellationRequested: false } && _scrollAnimationCts.Token == token) {
                _isProgrammaticScrolling = false;
                _scrollAnimationCts.Dispose();
                _scrollAnimationCts = null;
            }
        }
    }

    private double CalculateTargetOffset(Control container) {
        if (_scrollViewer == null)
            return 0;

        Rect itemBounds = container.Bounds;
        double targetOffset = itemBounds.Center.Y - _scrollViewer.Bounds.Height / 2;
        return Math.Clamp(targetOffset, 0, _scrollViewer.ScrollBarMaximum.Y);
    }

    private Task AnimateScrollToAsync(double targetOffset, CancellationToken cancellationToken) {
        if (_scrollViewer == null)
            return Task.CompletedTask;

        var animation = new Animation {
            Duration = ScrollAnimationDuration,
            Easing = ScrollEasing,
            FillMode = FillMode.Forward,
            Children = {
                new KeyFrame {
                    Cue = new Cue(0d),
                    Setters = { new Setter { Property = ScrollViewer.OffsetProperty, Value = _scrollViewer.Offset } }
                },
                new KeyFrame {
                    Cue = new Cue(1d),
                    Setters = {
                        new Setter {
                            Property = ScrollViewer.OffsetProperty,
                            Value = new Vector(_scrollViewer.Offset.X, targetOffset)
                        }
                    }
                }
            }
        };

        return animation.RunAsync(_scrollViewer, cancellationToken);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e) {
        base.OnDetachedFromVisualTree(e);

        DetachEvents();
        _scrollAnimationCts?.Cancel();
        _scrollAnimationCts?.Dispose();
        _scrollAnimationCts = null;
        _lyricItems.Clear();
        _lastHighlightedIndex = -1;
    }

    #region 依赖属性

    // 歌词数据
    public static readonly StyledProperty<LyricsData> LyricsDataProperty =
        AvaloniaProperty.Register<LyricsControl, LyricsData>(
            nameof(LyricsData),
            defaultValue: LyricsData.Loading,
            defaultBindingMode: BindingMode.OneWay);

    // 当前歌词索引
    public static readonly StyledProperty<int> CurrentLyricIndexProperty =
        AvaloniaProperty.Register<LyricsControl, int>(
            nameof(CurrentLyricIndex),
            -1,
            defaultBindingMode: BindingMode.OneWay);

    // 是否显示翻译
    public static readonly StyledProperty<bool> ShowTranslationProperty =
        AvaloniaProperty.Register<LyricsControl, bool>(nameof(ShowTranslation), true);

    // 歌词行高
    public static readonly StyledProperty<double> LineHeightProperty = AvaloniaProperty.Register<LyricsControl, double>(
        nameof(LineHeight) // 修改默认值为0，表示自动计算
    );

    // 歌词行间距
    public static readonly StyledProperty<double> LineSpacingProperty =
        AvaloniaProperty.Register<LyricsControl, double>(nameof(LineSpacing));

    // 滚动动画持续时间
    public static readonly StyledProperty<TimeSpan> ScrollAnimationDurationProperty =
        AvaloniaProperty.Register<LyricsControl, TimeSpan>(
            nameof(ScrollAnimationDuration),
            TimeSpan.FromMilliseconds(500));

    // 滚动缓动函数
    public static readonly StyledProperty<Easing> ScrollEasingProperty =
        AvaloniaProperty.Register<LyricsControl, Easing>(nameof(ScrollEasing), new CubicEaseOut());

    // 歌词文本对齐方式
    public static readonly StyledProperty<HorizontalAlignment> LyricTextAlignmentProperty =
        AvaloniaProperty.Register<LyricsControl, HorizontalAlignment>(
            nameof(LyricTextAlignment),
            HorizontalAlignment.Center);

    // 歌词文本边距
    public static readonly StyledProperty<Thickness> TextMarginProperty =
        AvaloniaProperty.Register<LyricsControl, Thickness>(nameof(TextMargin), new Thickness(20, 10));

    // 翻译文本间距
    public static readonly StyledProperty<double> TranslationSpacingProperty =
        AvaloniaProperty.Register<LyricsControl, double>(nameof(TranslationSpacing));

    // 点击的歌词时间点
    public static readonly StyledProperty<double> ClickedLyricTimeProperty =
        AvaloniaProperty.Register<LyricsControl, double>(
            nameof(ClickedLyricTime),
            defaultBindingMode: BindingMode.OneWayToSource);

    // 点击的歌词文本
    public static readonly StyledProperty<string> ClickedLyricTextProperty =
        AvaloniaProperty.Register<LyricsControl, string>(
            nameof(ClickedLyricText),
            string.Empty,
            defaultBindingMode: BindingMode.OneWayToSource);

    // 虚拟化列表数据源
    public static readonly DirectProperty<LyricsControl, IReadOnlyList<LyricLineViewItem>> LyricItemsProperty =
        AvaloniaProperty.RegisterDirect<LyricsControl, IReadOnlyList<LyricLineViewItem>>(
            nameof(LyricItems),
            x => x.LyricItems);

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

    [MaybeNull]
    public LyricsData LyricsData {
        get => GetValue(LyricsDataProperty);
        set => SetValue(LyricsDataProperty, value);
    }

    public int CurrentLyricIndex {
        get => GetValue(CurrentLyricIndexProperty);
        set => SetValue(CurrentLyricIndexProperty, value);
    }

    public bool ShowTranslation {
        get => GetValue(ShowTranslationProperty);
        set => SetValue(ShowTranslationProperty, value);
    }

    public double LineHeight {
        get => GetValue(LineHeightProperty);
        set => SetValue(LineHeightProperty, value);
    }

    public double LineSpacing {
        get => GetValue(LineSpacingProperty);
        set => SetValue(LineSpacingProperty, value);
    }

    public TimeSpan ScrollAnimationDuration {
        get => GetValue(ScrollAnimationDurationProperty);
        set => SetValue(ScrollAnimationDurationProperty, value);
    }

    public Easing ScrollEasing {
        get => GetValue(ScrollEasingProperty);
        set => SetValue(ScrollEasingProperty, value);
    }

    public HorizontalAlignment LyricTextAlignment {
        get => GetValue(LyricTextAlignmentProperty);
        set => SetValue(LyricTextAlignmentProperty, value);
    }

    public Thickness TextMargin {
        get => GetValue(TextMarginProperty);
        set => SetValue(TextMarginProperty, value);
    }

    public double TranslationSpacing {
        get => GetValue(TranslationSpacingProperty);
        set => SetValue(TranslationSpacingProperty, value);
    }

    public double ClickedLyricTime {
        get => GetValue(ClickedLyricTimeProperty);
        private set => SetValue(ClickedLyricTimeProperty, value);
    }

    public string ClickedLyricText {
        get => GetValue(ClickedLyricTextProperty);
        private set => SetValue(ClickedLyricTextProperty, value);
    }

    public IReadOnlyList<LyricLineViewItem> LyricItems => _lyricItems;

    #endregion

    #region 私有字段

    private readonly ObservableCollection<LyricLineViewItem> _lyricItems = [];
    private ListBox? _lyricsList;
    private ScrollViewer? _scrollViewer;
    private bool _isUserScrolling;
    private DateTime _lastUserScrollTime = DateTime.MinValue;
    private readonly TimeSpan _userScrollTimeout = TimeSpan.FromSeconds(3);
    private bool _isProgrammaticScrolling;
    private int _lastHighlightedIndex = -1;
    private CancellationTokenSource? _scrollAnimationCts;

    #endregion

    ~LyricsControl() {
        DetachEvents();
        LyricClicked = null;
    }
}

public sealed class LyricLineViewItem(
    double timePoint,
    string primaryText,
    string? translationText,
    bool hasTranslation,
    bool isPlaceholder,
    double lineHeight,
    Thickness margin) : IDisposable, INotifyPropertyChanged {
    public double TimePoint { get; } = timePoint;
    public string PrimaryText { get; } = primaryText;
    public string? TranslationText { get; } = translationText;
    public bool HasTranslation { get; } = hasTranslation;
    public bool IsPlaceholder { get; } = isPlaceholder;

    public bool ShowTranslation {
        get;
        set => SetField(ref field, value);
    }

    public bool IsCurrent {
        get;
        set => SetField(ref field, value);
    }

    public double LineHeight {
        get;
        set => SetField(ref field, value);
    } = lineHeight;

    public Thickness Margin {
        get;
        set => SetField(ref field, value);
    } = margin;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null) {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void Dispose() { PropertyChanged = null; }
}