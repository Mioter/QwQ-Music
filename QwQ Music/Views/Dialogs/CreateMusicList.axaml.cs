using Avalonia.Controls;
using Avalonia.Interactivity;

namespace QwQ_Music.Views.Dialogs;

public partial class CreateMusicList : Grid
{
    public CreateMusicList()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        NameTextBlock.Focus();
        base.OnLoaded(e);
    }
}
