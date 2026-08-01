using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using QwQ_Music.Models;

namespace QwQ_Music.Views.Customs;

/// <summary>
/// 桌面歌词双行控件：固定复用一组 ContentPresenter，以"传送带"方式平滑上滑显示两行歌词。
/// <para>
/// 支持 靠左 / 居中 / 靠右 三种对齐方式。
/// 当歌词间隔短于动画时长时，会临时创建额外的槽位以容纳重叠中的动画，动画结束后回收到基池（尽量复用）。
/// 切换歌曲时连续两次上滑：第一行（歌曲信息）立即上滑出现，第二行（副行）紧随其后。
/// 没有任何待驱动的动画时自动停止 DispatcherTimer，避免空转以降低资源消耗。
/// </para>
/// </summary>
[TemplatePart("PART_Container", typeof(Canvas), IsRequired = true)]
public class DualTransitioningContentControl : TemplatedControl {
    // ---------- 依赖属性 ----------
    public static readonly StyledProperty<LyricLinePair> ContentProperty =
        AvaloniaProperty.Register<DualTransitioningContentControl, LyricLinePair>(nameof(Content));

    public static readonly StyledProperty<IDataTemplate> ContentTemplateProperty =
        AvaloniaProperty.Register<DualTransitioningContentControl, IDataTemplate>(nameof(ContentTemplate));

    public static readonly StyledProperty<double> SpacingProperty =
        AvaloniaProperty.Register<DualTransitioningContentControl, double>(nameof(Spacing), 10.0);

    public static readonly StyledProperty<TimeSpan> DurationProperty =
        AvaloniaProperty.Register<DualTransitioningContentControl, TimeSpan>(
            nameof(Duration),
            TimeSpan.FromMilliseconds(300));

    public static readonly StyledProperty<TimeSpan> TickProperty =
        AvaloniaProperty.Register<DualTransitioningContentControl, TimeSpan>(
            nameof(Tick),
            TimeSpan.FromMilliseconds(16));

    public static readonly StyledProperty<HorizontalAlignment> AlignmentProperty =
        AvaloniaProperty.Register<DualTransitioningContentControl, HorizontalAlignment>(
            nameof(Alignment),
            HorizontalAlignment.Center);

    // ---------- CLR 属性 ----------
    public LyricLinePair Content {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    public IDataTemplate ContentTemplate {
        get => GetValue(ContentTemplateProperty);
        set => SetValue(ContentTemplateProperty, value);
    }

    public double Spacing {
        get => GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    public TimeSpan Duration {
        get => GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }
    
    public TimeSpan Tick {
        get => GetValue(TickProperty);
        set {
            if (Tick == value)
                return;
            SetValue(TickProperty, value);
            if (_timer == null)
                return;
            _timer.Stop();
            _timer.Interval = value;
            if (value != TimeSpan.Zero && HasActiveWork)
                _timer.Start();
        }
    }

    /// <summary>对齐方式（靠左 / 居中 / 靠右）。</summary>
    public HorizontalAlignment Alignment {
        get => GetValue(AlignmentProperty);
        set => SetValue(AlignmentProperty, value);
    }

    /// <summary>基池大小（必须 ≥ Visible，用于复用）。</summary>
    public required int Total { get; init; }

    /// <summary>同时可见的行数。</summary>
    public required int Visible { get; init; }

    private const double OffscreenY = -10000;

    /// <summary>单个显示槽位：一个复用控件 + 其动画状态。</summary>
    private sealed class Slot {
        public required ContentPresenter Presenter { get; init; }
        public LyricLine Line;
        public int QueueIndex = -1;        // 绑定的队列索引
        public bool Free = true;
        public bool Temp;                  // 超出基池临时创建的槽位
        public bool Exiting;               // 正在滑出顶部，动画结束后释放
        // Y 位移动画
        public bool Animating;
        public double FromY, ToY;
        public TimeSpan AnimStart;
    }

    private Canvas? _container;
    private readonly List<Slot> _slots = [];
    private readonly Stack<Slot> _freeSlots = [];
    private readonly List<Slot> _disposePending = [];
    private readonly List<LyricLine> _queue = [];
    private readonly Dictionary<LyricLine, double> _heights = [];
    private readonly Dictionary<LyricLine, double> _widths = [];
    private DispatcherTimer? _timer;
    private readonly Stopwatch _clock = new();
    private int _start;                    // 可见窗口的第一个队列索引
    private double _arrangeWidth = -1;
    private LyricLinePair? _pendingContent;
    private bool _templated;

    static DualTransitioningContentControl() {
        ContentProperty.Changed.AddClassHandler<DualTransitioningContentControl, LyricLinePair>((ctrl, args) =>
            ctrl.OnContentChanged(args.NewValue.Value));
    }

    // ---------- 生命周期 ----------

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e) {
        base.OnApplyTemplate(e);
        _container = e.NameScope.Find<Canvas>("PART_Container")!;
        if (Total <= 0 || Visible <= 0 || Total < Visible)
            throw new InvalidOperationException("Total must be >= Visible and both > 0.");

        _clock.Start();

        for (int i = 0; i < Total; i++) {
            var slot = CreateSlot();
            _slots.Add(slot);
            _freeSlots.Push(slot);
        }

        _templated = true;
        if (_pendingContent != null) {
            var pair = _pendingContent.Value;
            _pendingContent = null;
            OnContentChanged(pair);
        }
    }

    // ---------- 内容更新 ----------

    private void OnContentChanged(LyricLinePair pair) {
        if (!_templated) {
            _pendingContent = pair;
            return;
        }

        if (string.IsNullOrEmpty(pair.Primary.Primary)) {
            ResetAll();
            return;
        }

        bool isAppend = _queue.Count > 0 && _queue[^1] == pair.Primary;
        if (isAppend) {
            if (pair.Alternate != LyricLine.Empty)
                _queue.Add(pair.Alternate);
        } else {
            // 切换歌曲 / 跳转：清空后重现。第一行立即出现，第二行延迟一次动画时长形成连续两次上滑。
            ResetAll();
            _queue.Add(pair.Primary);
            if (pair.Alternate != LyricLine.Empty && pair.Alternate != pair.Primary)
                _queue.Add(pair.Alternate);
        }

        TimeSpan newLineDelay = isAppend || _queue.Count < 2 ? TimeSpan.Zero : Duration;
        UpdateSlideMode(newLineDelay);

        InvalidateMeasure();
        InvalidateArrange();
    }

    /// <summary>所有槽位绑定各自的队列行，随传送带上移，退出时滑出顶部。</summary>
    private void UpdateSlideMode(TimeSpan newLineDelay) {
        int count = _queue.Count;
        _start = Math.Max(0, count - Visible);

        // 1. 重设已有可见槽位 / 标记退场槽位
        for (int i = 0; i < _slots.Count; i++) {
            var slot = _slots[i];
            if (slot.Free || slot.QueueIndex < 0)
                continue;
            int qi = slot.QueueIndex;
            if (qi < _start) {
                if (!slot.Exiting) {
                    slot.Exiting = true;
                    StartAnimY(slot, CurrentY(slot), -HeightOf(slot.Line) - Spacing, TimeSpan.Zero);
                }
            } else {
                slot.Exiting = false;
                RetargetY(slot, CumulativeTarget(qi));
            }
        }

        // 2. 为可见窗口内缺失的行获取槽位
        for (int i = _start; i < count; i++) {
            if (FindSlotForQueueIndex(i) != null)
                continue;
            var slot = AcquireSlot();
            slot.Line = _queue[i];
            slot.QueueIndex = i;
            slot.Presenter.Content = _queue[i];
            EnsureMeasured(slot);

            double target = CumulativeTarget(i);
            double shift = count <= Visible ? HeightOf(_queue[i]) + Spacing : HeightOf(_queue[_start - 1]) + Spacing;
            double startY = target + shift;
            TimeSpan delay = i == count - 1 ? newLineDelay : TimeSpan.Zero;
            SetPos(slot, startY);
            StartAnimY(slot, startY, target, delay);
            UpdateX(slot);
        }

        EnsureTimer();
    }

    private void ResetAll() {
        _queue.Clear();
        _start = 0;
        for (int i = _slots.Count - 1; i >= 0; i--) {
            var slot = _slots[i];
            if (!slot.Free)
                ReleaseSlot(slot);
        }
        DrainDispose();
    }

    // ---------- 池管理 ----------

    private Slot CreateSlot() {
        var presenter = new ContentPresenter {
            ContentTemplate = ContentTemplate,
            RenderTransform = new TranslateTransform(),
            IsVisible = false
        };
        _container!.Children.Add(presenter);
        return new Slot { Presenter = presenter };
    }

    private Slot AcquireSlot() {
        if (_freeSlots.TryPop(out var slot)) {
            slot.Free = false;
            slot.Presenter.IsVisible = true;
            return slot;
        }
        var created = CreateSlot();
        _slots.Add(created);
        created.Temp = _slots.Count > Total;
        created.Free = false;
        created.Presenter.IsVisible = true;
        return created;
    }

    private void ReleaseSlot(Slot slot) {
        slot.Free = true;
        slot.Animating = false;
        slot.Exiting = false;
        slot.Line = default;
        slot.QueueIndex = -1;
        slot.Presenter.Content = null;
        slot.Presenter.IsVisible = false;
        SetPos(slot, OffscreenY);
        if (slot.Temp)
            _disposePending.Add(slot); // 临时槽位延迟销毁，避免遍历中修改集合
        else
            _freeSlots.Push(slot);
    }

    private void DrainDispose() {
        if (_disposePending.Count == 0)
            return;
        foreach (var slot in _disposePending) {
            _slots.Remove(slot);
            _container?.Children.Remove(slot.Presenter);
        }
        _disposePending.Clear();
    }

    // ---------- 动画 ----------

    private void StartAnimY(Slot slot, double fromY, double toY, TimeSpan delay) {
        slot.FromY = fromY;
        slot.ToY = toY;
        slot.AnimStart = _clock.Elapsed + delay;
        slot.Animating = true;
        SetPos(slot, fromY);
    }

    private void RetargetY(Slot slot, double target) {
        if (slot.Animating && Math.Abs(slot.ToY - target) < 0.01)
            return;
        double from = CurrentY(slot);
        slot.FromY = from;
        slot.ToY = target;
        slot.AnimStart = _clock.Elapsed;
        slot.Animating = Math.Abs(from - target) > 0.01;
        SetPos(slot, from);
    }

    private double CurrentY(Slot slot) {
        if (!slot.Animating)
            return GetPos(slot);
        double t = (_clock.Elapsed - slot.AnimStart).TotalMilliseconds / Math.Max(1, Duration.TotalMilliseconds);
        if (t <= 0)
            return slot.FromY;
        if (t >= 1)
            return slot.ToY;
        return slot.FromY + (slot.ToY - slot.FromY) * Ease(t);
    }

    private void OnTick(object? sender, EventArgs e) {
        var now = _clock.Elapsed;

        for (int i = 0; i < _slots.Count; i++) {
            var slot = _slots[i];
            if (slot.Free)
                continue;

            // Y 位移
            if (slot.Animating) {
                double t = (now - slot.AnimStart).TotalMilliseconds / Math.Max(1, Duration.TotalMilliseconds);
                if (t >= 1) {
                    slot.Animating = false;
                    SetPos(slot, slot.ToY);
                    if (slot.Exiting) {
                        ReleaseSlot(slot);
                        continue;
                    }
                } else if (t > 0) {
                    SetPos(slot, slot.FromY + (slot.ToY - slot.FromY) * Ease(t));
                } else {
                    SetPos(slot, slot.FromY);
                }
            }
        }

        // 没有任何剩余动画时立即停用计时器，空闲期间不再空转消耗资源
        if (!HasActiveWork)
            _timer?.Stop();
        DrainDispose();
    }

    private void EnsureTimer() {
        if (Tick == TimeSpan.Zero)
            return;
        if (!HasActiveWork)
            return; // 没有需要驱动的动画，不启动计时器
        _timer ??= new DispatcherTimer(Tick, DispatcherPriority.Render, OnTick);
        if (!_timer.IsEnabled)
            _timer.Start();
    }

    /// <summary>是否仍有未完成的位移动画；为 false 时无需运行计时器。</summary>
    private bool HasActiveWork {
        get {
            for (int i = 0; i < _slots.Count; i++) {
                var s = _slots[i];
                if (!s.Free && s.Animating)
                    return true;
            }
            return false;
        }
    }

    // ---------- 位置 / 测量 ----------

    private double CumulativeTarget(int queueIndex) {
        double target = 0;
        for (int j = _start; j < queueIndex; j++)
            target += HeightOf(_queue[j]) + Spacing;
        return target;
    }

    private void UpdateX(Slot slot) {
        if (slot.Free || _arrangeWidth <= 0)
            return;
        double w = WidthOf(slot.Line);
        double x = Alignment switch {
            HorizontalAlignment.Right => Math.Max(0, _arrangeWidth - w),
            HorizontalAlignment.Center => Math.Max(0, (_arrangeWidth - w) / 2),
            _ => 0
        };
        if (slot.Presenter.RenderTransform is TranslateTransform t)
            t.X = x;
    }

    private void SetPos(Slot slot, double value) {
        if (slot.Presenter.RenderTransform is TranslateTransform t)
            t.Y = value;
    }

    private double GetPos(Slot slot) {
        return slot.Presenter.RenderTransform is TranslateTransform t ? t.Y : 0;
    }

    private void EnsureMeasured(Slot slot) {
        slot.Presenter.Measure(Size.Infinity);
        var size = slot.Presenter.DesiredSize;
        _heights[slot.Line] = size.Height;
        _widths[slot.Line] = size.Width;
    }

    private double HeightOf(LyricLine line) => _heights.TryGetValue(line, out double h) ? h : 0;

    private double WidthOf(LyricLine line) => _widths.TryGetValue(line, out double w) ? w : 0;

    private Slot? FindSlotForQueueIndex(int queueIndex) {
        for (int i = 0; i < _slots.Count; i++) {
            var s = _slots[i];
            if (!s.Free && s.QueueIndex == queueIndex)
                return s;
        }
        return null;
    }

    private Slot? FindSlotForLine(LyricLine line) {
        for (int i = 0; i < _slots.Count; i++) {
            var s = _slots[i];
            if (!s.Free && s.Line == line)
                return s;
        }
        return null;
    }

    protected override Size MeasureOverride(Size availableSize) {
        if (_container == null)
            return base.MeasureOverride(availableSize);

        double width = 0, height = 0;
        int count = _queue.Count;
        int start = Math.Max(0, count - Visible);
        for (int i = start; i < count; i++) {
            var line = _queue[i];
            if (HeightOf(line) <= 0 || WidthOf(line) <= 0) {
                var slot = FindSlotForLine(line);
                if (slot != null)
                    EnsureMeasured(slot);
            }
            double h = HeightOf(line);
            double w = WidthOf(line);
            if (i > start)
                height += Spacing;
            height += h;
            width = Math.Max(width, w);
        }
        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize) {
        base.ArrangeOverride(finalSize);
        _arrangeWidth = finalSize.Width;
        for (int i = 0; i < _slots.Count; i++) {
            var slot = _slots[i];
            if (!slot.Free)
                UpdateX(slot);
        }
        return finalSize;
    }

    // ---------- 属性变更 ----------

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change) {
        base.OnPropertyChanged(change);

        if (change.Property == ContentTemplateProperty) {
            foreach (var slot in _slots)
                slot.Presenter.ContentTemplate = ContentTemplate;
        } else if (change.Property == AlignmentProperty) {
            InvalidateArrange();
        } else if (change.Property == SpacingProperty) {
            if (_templated && _queue.Count > 0) {
                UpdateSlideMode(TimeSpan.Zero);
                InvalidateMeasure();
                InvalidateArrange();
            }
        }
        // Duration 变化由后续动画自然采用，无需特殊处理。
    }

    private static double Ease(double t) {
        t = Math.Clamp(t, 0, 1);
        return t * t * (3 - 2 * t); // smoothstep
    }
}
