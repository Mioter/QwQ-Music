using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using QwQ_Music.Common;

namespace QwQ_Music.Views.UserControls;

public partial class FontSelector : Grid
{
    public FontSelector()
    {
        InitializeComponent();
        DataContext = this;
    }

    public static AppResources AppResources => AppResources.Default;

    public static readonly DirectProperty<FontSelector, string?> SelectedFontProperty =
        AvaloniaProperty.RegisterDirect<FontSelector, string?>(
            nameof(SelectedFont),
            o => o.SelectedFont,
            (o, v) => o.SelectedFont = v,
            defaultBindingMode: BindingMode.TwoWay);

    public string? SelectedFont
    {
        get;
        set
        {
            if (value != null && value != field)
                SetAndRaise(SelectedFontProperty, ref field, value);
        }
    } = AppResources.DEFAULT_FONT_KEY;
}
