using Avalonia.Controls;
using Avalonia.Interactivity;

namespace QwQ_Music.Views.UserControls;

public partial class CreateMusicList : UserControl
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
