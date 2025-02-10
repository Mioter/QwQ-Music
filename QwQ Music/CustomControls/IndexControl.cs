using System.Linq;
using Avalonia;
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

    public static readonly StyledProperty<object?> DefaultContentProperty =
        AvaloniaProperty.Register<IndexControl, object?>(nameof(DefaultContent));

    private ContentControl? _contentControl;

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
