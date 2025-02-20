using System.Linq;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;

namespace QwQ_Music.CustomControls;

public class IndexControl : ItemsControl
{
    // 定义依赖属性
    public static readonly StyledProperty<int> IndexProperty =
        AvaloniaProperty.Register<IndexControl, int>(
            nameof(Index),
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<object?> DefaultContentProperty =
        AvaloniaProperty.Register<IndexControl, object?>(nameof(DefaultContent));

    public static readonly StyledProperty<IPageTransition?> PageTransitionProperty =
        AvaloniaProperty.Register<IndexControl, IPageTransition?>(nameof(PageTransition));

    private TransitioningContentControl? _transitioningContent;

    static IndexControl()
    {
        IndexProperty.Changed.AddClassHandler<IndexControl>((x, _) => x.UpdateContent());
    }

    public int Index
    {
        get => GetValue(IndexProperty);
        set => SetValue(IndexProperty, value);
    }

    public object? DefaultContent
    {
        get => GetValue(DefaultContentProperty);
        set => SetValue(DefaultContentProperty, value);
    }

    public IPageTransition? PageTransition
    {
        get => GetValue(PageTransitionProperty);
        set => SetValue(PageTransitionProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        // 查找模板中的 TransitioningContentControl
        _transitioningContent = e.NameScope.Find<TransitioningContentControl>("PART_TransitioningContent");

        // 绑定 PageTransition 属性
        _transitioningContent?.Bind(TransitioningContentControl.PageTransitionProperty,
            this.GetObservable(PageTransitionProperty));

        UpdateContent();
    }

    private void UpdateContent()
    {
        if (_transitioningContent == null) return;

        var items = Items.Cast<object>().ToList();
        int targetIndex = Index;

        // 获取目标内容
        object? newContent = targetIndex >= 0 && targetIndex < items.Count
            ? items[targetIndex]
            : DefaultContent ?? CreateDefaultFallback();

        // 更新内容
        _transitioningContent.Content = newContent;
    }

    private static TextBlock CreateDefaultFallback()
    {
        return new TextBlock
        {
            Text = "Invalid selection",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = Brushes.Red,
        };
    }
}