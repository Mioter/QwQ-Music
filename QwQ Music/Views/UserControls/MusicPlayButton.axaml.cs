using Avalonia.Controls;
using QwQ_Music.ViewModels.UserControls;

namespace QwQ_Music.Views.UserControls;

public partial class MusicPlayButton : Button
{
    public MusicPlayButton()
    {
        InitializeComponent();
        DataContext = new MusicPlayButtonViewModel();
    }
}
