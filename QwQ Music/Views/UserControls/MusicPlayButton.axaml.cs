using Avalonia.Controls;
using QwQ_Music.ViewModels;

namespace QwQ_Music.Views.UserControls;

public partial class MusicPlayButton : UserControl
{
    public MusicPlayButton()
    {
        InitializeComponent();
        DataContext = new MusicPlayButtonViewModel();
    }
}
