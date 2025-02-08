using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Layout;

namespace QwQ_Music.CustomControls;

public class IndexControl : ItemsControl
{
    public static readonly StyledProperty<int> IndexProperty =
        AvaloniaProperty.Register<IndexControl, int>(
            nameof(Index),
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<IPageTransition> TransitionProperty =
        AvaloniaProperty.Register<IndexControl, IPageTransition>(nameof(Transition));

    public static readonly StyledProperty<object?> DefaultContentProperty =
        AvaloniaProperty.Register<IndexControl, object?>(nameof(DefaultContent));
    private ContentControl? _contentControl;

    private CancellationTokenSource? _cts;
    private int _lastIndex;

    static IndexControl()
    {
        IndexProperty.Changed.AddClassHandler<IndexControl>((x, _) => x.UpdateContent());
    }

    public int Index
    {
        get => GetValue(IndexProperty);
        set => SetValue(IndexProperty, value);
    }

    public IPageTransition Transition
    {
        get => GetValue(TransitionProperty);
        set => SetValue(TransitionProperty, value);
    }

    public object? DefaultContent
    {
        get => GetValue(DefaultContentProperty);
        set => SetValue(DefaultContentProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _contentControl = e.NameScope.Find<ContentControl>("PART_ContentControl");
        UpdateContent();
    }

    private void UpdateContent()
    {
        if (_contentControl == null) return;

        var items = Items.Cast<object>().ToList();
        int targetIndex = Index;

        object? newContent = targetIndex >= 0 && targetIndex < items.Count
            ? items[targetIndex]
            : DefaultContent ?? CreateDefaultFallback();

        if (_contentControl.Content is Visual oldVisual && newContent is Visual newVisual)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            bool forward = Index > _lastIndex;
            _lastIndex = Index;

            Transition.Start(oldVisual, newVisual, forward, _cts.Token);
        }

        _contentControl.Content = newContent;
    }

    private static TextBlock CreateDefaultFallback()
    {
        return new TextBlock
        {
            Text = "Invalid selection",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
    }
}
