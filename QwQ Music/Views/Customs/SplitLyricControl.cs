using System.Diagnostics;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Threading;
using QwQ_Music.Common.Services;
using QwQ_Music.Models;
using Timer = System.Timers.Timer;

namespace QwQ_Music.Views.Customs;

/// <summary>
/// 分屏歌词控件：固定两个槽位交替显示当前句与上一句。
/// <para>
/// 属性变更统一调度到 UI 线程按到达顺序处理；淡入淡出由单个
/// <see cref="DispatcherTimer"/> 驱动的状态机完成，可被新更新随时打断并
/// 平滑转向最新内容，避免并发交错与闪烁。
/// </para>
/// </summary>
public class SplitLyricControl : TemplatedControl {
    // ===== 依赖属性 =====
    public static readonly StyledProperty<LyricLinePair> CurrentProperty =
        AvaloniaProperty.Register<SplitLyricControl, LyricLinePair>(nameof(Current));

    public static readonly StyledProperty<TimeSpan> DurationProperty =
        AvaloniaProperty.Register<SplitLyricControl, TimeSpan>(
            nameof(Duration),
            defaultValue: TimeSpan.FromMilliseconds(300));

    public static readonly StyledProperty<double> SpacingProperty =
        AvaloniaProperty.Register<SplitLyricControl, double>(nameof(Spacing), defaultValue: 10);

    public static readonly StyledProperty<bool> ShowTranslationProperty =
        AvaloniaProperty.Register<SplitLyricControl, bool>(nameof(ShowTranslation), defaultValue: false);

    /// <summary>第一（主）槽位的显示模板。</summary>
    public static readonly StyledProperty<IDataTemplate?> FirstContentTemplateProperty =
        AvaloniaProperty.Register<SplitLyricControl, IDataTemplate?>(nameof(FirstContentTemplate));

    /// <summary>第二（副）槽位的显示模板。</summary>
    public static readonly StyledProperty<IDataTemplate?> SecondContentTemplateProperty =
        AvaloniaProperty.Register<SplitLyricControl, IDataTemplate?>(nameof(SecondContentTemplate));

    // ===== 属性包装 =====
    public LyricLinePair Current {
        get => GetValue(CurrentProperty);
        set => SetValue(CurrentProperty, value);
    }

    public TimeSpan Duration {
        get => GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }

    public double Spacing {
        get => GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    public bool ShowTranslation {
        get => GetValue(ShowTranslationProperty);
        set => SetValue(ShowTranslationProperty, value);
    }

    public IDataTemplate? FirstContentTemplate {
        get => GetValue(FirstContentTemplateProperty);
        set => SetValue(FirstContentTemplateProperty, value);
    }

    public IDataTemplate? SecondContentTemplate {
        get => GetValue(SecondContentTemplateProperty);
        set => SetValue(SecondContentTemplateProperty, value);
    }

    private record SlotStatus {
        public ContentControl? Slot;
        public LyricLine Cache = LyricLine.Empty;
        public LyricLine Pending = LyricLine.Empty;
        public readonly DispatcherTimer Timer = new(DispatcherPriority.Background, Dispatcher.UIThread);
    }

    private readonly SlotStatus _slot1 = new();
    private readonly SlotStatus _slot2 = new();

    // ===== 静态构造 =====
    static SplitLyricControl() {
        CurrentProperty.Changed.AddClassHandler<SplitLyricControl>((o, _) => o.OnCurrentChanged());
        FirstContentTemplateProperty.Changed.AddClassHandler<SplitLyricControl>((o, _) => o.ApplySlotTemplates());
        SecondContentTemplateProperty.Changed.AddClassHandler<SplitLyricControl>((o, _) => o.ApplySlotTemplates());
    }

    // ===== 模板应用 =====
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e) {
        base.OnApplyTemplate(e);
        _slot1.Slot = e.NameScope.Find<ContentControl>("PART_PrimarySlot");
        _slot2.Slot = e.NameScope.Find<ContentControl>("PART_AlternateSlot");
        ApplySlotTemplates();
        ResetSlotTransition(_slot1);
        ResetSlotTransition(_slot2);
    }


    private void ApplySlotTemplates() {
        if (_slot1.Slot != null && FirstContentTemplate != null)
            _slot1.Slot.ContentTemplate = FirstContentTemplate;
        if (_slot2.Slot != null && SecondContentTemplate != null)
            _slot2.Slot.ContentTemplate = SecondContentTemplate;
    }

    // ===== Current 变化处理 =====
    private void OnCurrentChanged() {
        if (!IsVisible || _slot1.Slot == null || _slot2.Slot == null)
            return;

        if (_slot1.Cache == LyricLine.Empty ||                                   // 初始化
            (_slot1.Cache != Current.Primary && _slot2.Cache != Current.Primary) // 跳转
           ) {
            _slot1.Pending = Current.Primary;
            _slot2.Pending = Current.Alternate;
            Dispatcher.UIThread.Post(
                _ => {
                    BeginAnimationOnSlot(_slot1);
                    BeginAnimationOnSlot(_slot2);
                },
                DispatcherPriority.Render);
        } else if (_slot1.Cache == Current.Primary) {
            _slot2.Pending = Current.Alternate;
            BeginAnimationOnSlot(_slot2);
        } else if (_slot2.Cache == Current.Primary) {
            _slot1.Pending = Current.Alternate;
            BeginAnimationOnSlot(_slot1);
        }
    }

    private void BeginAnimationOnSlot(SlotStatus slot) {
        if (slot.Slot is null)
            return;
        slot.Timer.Interval = Duration;

        if (!slot.Timer.IsEnabled) {
            slot.Slot.Opacity = 0;
            slot.Timer.Tick += Update;
        } else {
            ResetSlotTransition(slot, 0);
            slot.Cache = slot.Pending;
            slot.Slot.Content = slot.Cache;
            slot.Slot.Opacity = 1;
        }

        slot.Timer.Start();

        return;

        void Update(object? sender, EventArgs e) {
            if (slot.Slot.Opacity < 0.01) {
                Dispatcher.UIThread.Post(() => {
                    slot.Cache = slot.Pending;
                    slot.Slot.Content = slot.Cache;
                    slot.Slot.Opacity = 1;
                });
            } else {
                slot.Timer.Stop();
            }
        }
    }

    private void ResetSlotTransition(SlotStatus slot, double? opacity = null) {
        if (slot.Slot is null)
            return;
        if (slot.Slot.Transitions?.OfType<DoubleTransition>().FirstOrDefault(item => item.Property == OpacityProperty)
            is { } transition)
            slot.Slot.Transitions?.Remove(transition);
        if (opacity is { } opacity1)
            slot.Slot.Opacity = opacity1;
        slot.Slot.Transitions ??= [];
        slot.Slot.Transitions.Add(new DoubleTransition { Property = OpacityProperty, Duration = Duration });
    }
}