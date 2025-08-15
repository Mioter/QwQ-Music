using Avalonia.Controls;
using Avalonia.Interactivity;

namespace QwQ_Music.Views.Dialogs;

public partial class KeyGestureInput : Grid
{
    public KeyGestureInput()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        GestureInputInDialog.Focus();
    }
}
