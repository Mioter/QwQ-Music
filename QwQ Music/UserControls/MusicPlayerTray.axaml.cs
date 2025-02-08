using Avalonia.Controls;
using QwQ_Music.ViewModels;

namespace QwQ_Music.UserControls;

public partial class MusicPlayerTray : UserControl
{
    public MusicPlayerTray()
    {
        InitializeComponent();
        DataContext = new MusicPlayerTrayViewModel();
    }
}
