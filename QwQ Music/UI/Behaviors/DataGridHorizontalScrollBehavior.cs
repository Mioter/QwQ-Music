using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Xaml.Interactivity;
using Irihi.Avalonia.Shared.Helpers;

namespace QwQ_Music.UI.Behaviors;

public class DataGridHorizontalScrollBehavior : Behavior<DataGrid> {
    // 附加属性：用于外部绑定
    public static readonly StyledProperty<double> HorizontalScrollValueProperty =
        AvaloniaProperty.Register<DataGridHorizontalScrollBehavior, double>(
            nameof(HorizontalScrollValue),
            defaultBindingMode: BindingMode.TwoWay);

    // 只读属性：用于获取滚动条的最大值
    public static readonly StyledProperty<double> HorizontalScrollMaximumProperty =
        AvaloniaProperty.Register<DataGridHorizontalScrollBehavior, double>(nameof(HorizontalScrollMaximum));

    private ScrollBar? _horizontalScrollBar;
    private bool _isUpdatingFromScrollBar;

    public double HorizontalScrollValue {
        get => GetValue(HorizontalScrollValueProperty);
        set => SetValue(HorizontalScrollValueProperty, value);
    }

    public double HorizontalScrollMaximum {
        get => GetValue(HorizontalScrollMaximumProperty);
        private set => SetValue(HorizontalScrollMaximumProperty, value);
    }

    protected override void OnAttached() {
        base.OnAttached();

        if (AssociatedObject != null) {
            AssociatedObject.TemplateApplied += OnTemplateApplied;
            // 监听我们自己的属性变化，用于反向同步到滚动条
            this.GetObservable(HorizontalScrollValueProperty).Subscribe(OnHorizontalScrollValueChanged);
        }
    }

    protected override void OnDetaching() {
        if (AssociatedObject != null)
            AssociatedObject.TemplateApplied -= OnTemplateApplied;

        if (_horizontalScrollBar != null)
            _horizontalScrollBar.ValueChanged -= OnScrollBarValueChanged;

        base.OnDetaching();
    }

    private void OnTemplateApplied(object? sender, TemplateAppliedEventArgs e) {
        // 找到水平滚动条
        _horizontalScrollBar = e.NameScope.Find<ScrollBar>("PART_VerticalScrollbar");

        if (_horizontalScrollBar != null) {
            // 监听滚动条的值变化
            _horizontalScrollBar.ValueChanged += OnScrollBarValueChanged;

            // 更新最大值
            HorizontalScrollMaximum = _horizontalScrollBar.Maximum;

            // 同步当前值
            _isUpdatingFromScrollBar = true;
            HorizontalScrollValue = _horizontalScrollBar.Value;
            _isUpdatingFromScrollBar = false;
        }
    }

    private void OnScrollBarValueChanged(object? sender, RangeBaseValueChangedEventArgs e) {
        if (_isUpdatingFromScrollBar)
            return;

        _isUpdatingFromScrollBar = true;
        HorizontalScrollValue = e.NewValue;
        _isUpdatingFromScrollBar = false;
    }

    private void OnHorizontalScrollValueChanged(double newValue) {
        if (_horizontalScrollBar == null || _isUpdatingFromScrollBar)
            return;

        _isUpdatingFromScrollBar = true;
        _horizontalScrollBar.Value = newValue;
        _isUpdatingFromScrollBar = false;
    }
}