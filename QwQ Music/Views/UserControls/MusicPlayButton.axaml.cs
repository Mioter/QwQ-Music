using Avalonia.Controls;
using MusicPlayButtonViewModel = QwQ_Music.ViewModels.UserControls.MusicPlayButtonViewModel;

namespace QwQ_Music.Views.UserControls;

public partial class MusicPlayButton : UserControl
{
    public MusicPlayButton()
    {
        InitializeComponent();
        DataContext = new MusicPlayButtonViewModel();
    }
}
