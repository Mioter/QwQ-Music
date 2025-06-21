using Avalonia.Controls;
using Avalonia.Interactivity;

namespace QwQ_Music.Views.UserControls;

public partial class KeyGestureInputDialog : UserControl
{
    public KeyGestureInputDialog()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        GestureInputInDialog.Focus();
    }
}
