using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using QwQ_Music.Models;

namespace QwQ_Music.Views.Customs;

/// <summary>
/// 自定义控件，使用固定数量的 ContentPresenter 循环显示歌词行，支持滑动过渡动画。
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

    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<DualTransitioningContentControl, Orientation>(
            nameof(Orientation),
            Orientation.Vertical);

    public static readonly StyledProperty<TimeSpan> DurationProperty =
        AvaloniaProperty.Register<DualTransitioningContentControl, TimeSpan>(
            nameof(Duration),
            TimeSpan.FromMilliseconds(300));

    public static readonly StyledProperty<int> BatchProperty =
        AvaloniaProperty.Register<DualTransitioningContentControl, int>(nameof(Batch));

    // ---------- CLR 属性 ----------
    public LyricLinePair? Content {
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

    public Orientation Orientation {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    public TimeSpan Duration {
        get => GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }

    public int Batch {
        get => GetValue(BatchProperty);
        set => SetValue(BatchProperty, value);
    }

    // 池大小（必须 >= Visible）
    public required int Total { get; init; }

    // 可见行数
    public required int Visible { get; init; }

    // ---------- 内部字段 ----------
    private Canvas? _container;
    private ContentPresenter[]? _presenters;
    private readonly List<LyricLine> _queue = [];
    private bool _isUpdating;

    // ---------- 构造函数 ----------
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

        _presenters = new ContentPresenter[Total];
        _container.Children.Clear();

        for (int i = 0; i < Total; i++) {
            var presenter = new ContentPresenter {
                [!ContentPresenter.ContentTemplateProperty] =
                    CompiledBinding.Create<DualTransitioningContentControl, IDataTemplate>(
                        c => c.ContentTemplate,
                        this),
                RenderTransform = new TranslateTransform() // 使用 TranslateTransform 控制位置
            };
            _container.Children.Add(presenter);
            _presenters[i] = presenter;

            // 初始位置设在外部
            SetPosition(presenter, -10000);
        }
    }

    // ---------- 内容更新 ----------
    private void OnContentChanged(LyricLinePair? pair) {
        if (pair == null || _presenters == null || _container == null || _isUpdating)
            return;

        _isUpdating = true;
        bool isAppend = _queue.LastOrDefault() == pair.Value.Primary;
        try {
            if (!isAppend) {
                _queue.Clear();
                _queue.Add(pair.Value.Primary);
            }

            _queue.Add(pair.Value.Alternate);

            int startIndex = Math.Max(0, _queue.Count - Visible);


            // 2. 清空模式：将所有槽位移到外部（无动画）
            if (!isAppend) {
                foreach (ContentPresenter presenter in _presenters) {
                    if (presenter.RenderTransform is TranslateTransform transform) {
                        transform.Transitions = null; // 立即停止动画
                    }
                }

                for (int i = 0; i < Total; i++) {
                    _presenters[i].Content = null;
                    SetPosition(_presenters[i], -10000);
                }
            }

            // 3. 计算目标内容和目标位置
            var targetPositions = new double[Total];
            var targetContents = new LyricLine[Total];
            for (int slot = 0; slot < Total; slot++) {
                int queueIndex = startIndex + slot;
                if (queueIndex < _queue.Count) {
                    targetContents[slot] = _queue[queueIndex];
                } else {
                    targetContents[slot] = LyricLine.Empty;
                    targetPositions[slot] = -10000;
                }
            }

            double cumulative = 0;
            for (int slot = 0; slot < Total; slot++) {
                _presenters[slot].Content = targetContents[slot];
                _presenters[slot].Measure(Size.Infinity);
                var desired = _presenters[slot].DesiredSize;

                if (targetContents[slot] != LyricLine.Empty) {
                    targetPositions[slot] = cumulative;
                    cumulative += (Orientation == Orientation.Vertical ? desired.Height : desired.Width) + Spacing;
                } else {
                    targetPositions[slot] = -10000;
                }
            }

            // 5. 启动动画：为每个槽位设置 Transitions，然后修改位置属性
            for (int slot = 0; slot < Total; slot++) {
                var presenter = _presenters[slot];
                if (presenter.RenderTransform is not TranslateTransform transform)
                    continue;

                double current = GetPosition(presenter);
                double target = targetPositions[slot];

                if (Math.Abs(current - target) < 0.01) {
                    SetPosition(presenter, target);
                    continue;
                }

                var property = Orientation == Orientation.Vertical ?
                    TranslateTransform.YProperty :
                    TranslateTransform.XProperty;

                // 创建过渡动画
                var transition = new DoubleTransition { Duration = Duration, Property = property };

                // 替换 Transitions，这会终止之前的动画
                transform.Transitions = [transition];

                // 现在修改 Y 或 X，DoubleTransition 会自动捕捉变化并播放动画
                if (Orientation == Orientation.Vertical)
                    transform.Y = target;
                else
                    transform.X = target;
            }

            InvalidateMeasure();
        } finally {
            _isUpdating = false;
        }
    }

    // ---------- 辅助方法 ----------
    private void SetPosition(ContentPresenter presenter, double value) {
        if (presenter.RenderTransform is not TranslateTransform transform)
            return;

        if (Orientation == Orientation.Vertical)
            transform.Y = value;
        else
            transform.X = value;
    }

    private double GetPosition(ContentPresenter presenter) {
        if (presenter.RenderTransform is not TranslateTransform transform)
            return 0;

        return Orientation == Orientation.Vertical ? transform.Y : transform.X;
    }

    // ---------- 布局重写 ----------
    protected override Size MeasureOverride(Size availableSize) {
        if (_presenters == null || _container == null)
            return base.MeasureOverride(availableSize);

        double totalWidth = 0, totalHeight = 0;

        foreach (var presenter in _presenters) {
            presenter.Measure(Size.Infinity);
            var size = presenter.DesiredSize;
            double pos = GetPosition(presenter);

            // 只计入位置在合理范围内的可见槽位
            if (pos >= -5000) {
                if (Orientation == Orientation.Vertical) {
                    totalWidth = Math.Max(totalWidth, size.Width);
                    totalHeight += size.Height + Spacing;
                } else {
                    totalHeight = Math.Max(totalHeight, size.Height);
                    totalWidth += size.Width + Spacing;
                }
            }
        }

        // 减去最后一个多余的 Spacing
        if (Orientation == Orientation.Vertical && totalHeight > 0)
            totalHeight -= Spacing;
        else if (Orientation == Orientation.Horizontal && totalWidth > 0)
            totalWidth -= Spacing;

        return new Size(totalWidth, totalHeight);
    }

    // ---------- 属性变更处理 ----------
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change) {
        base.OnPropertyChanged(change);

        if (change.Property == SpacingProperty ||
            change.Property == OrientationProperty ||
            change.Property == DurationProperty) {
            if (Content != null)
                OnContentChanged(Content);
        }
    }
}