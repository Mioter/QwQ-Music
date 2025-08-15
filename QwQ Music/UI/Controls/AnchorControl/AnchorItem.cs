using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace QwQ_Music.UI.Controls.AnchorControl;

public class AnchorItem : ContentControl
{
    public static readonly StyledProperty<object?> HeaderProperty =
        AvaloniaProperty.Register<AnchorItem, object?>(nameof(Header));

    public static readonly StyledProperty<double> HeaderHeightProperty =
        AvaloniaProperty.Register<AnchorItem, double>(nameof(HeaderHeight), 40d, true);

    public static readonly StyledProperty<IDataTemplate?> HeaderTemplateProperty =
        AvaloniaProperty.Register<AnchorItem, IDataTemplate?>(nameof(HeaderTemplate), inherits: true);

    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public double HeaderHeight
    {
        get => GetValue(HeaderHeightProperty);
        set => SetValue(HeaderHeightProperty, value);
    }

    public IDataTemplate? HeaderTemplate
    {
        get => GetValue(HeaderTemplateProperty);
        set => SetValue(HeaderTemplateProperty, value);
    }
}
