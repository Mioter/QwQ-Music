using Avalonia.Controls;
using MusicPlayerTrayViewModel = QwQ_Music.ViewModels.UserControls.MusicPlayerTrayViewModel;

namespace QwQ_Music.Views.UserControls;

public partial class MusicPlayerTray : UserControl
{
    public MusicPlayerTray()
    {
        InitializeComponent();
        DataContext = new MusicPlayerTrayViewModel();
    }
}
