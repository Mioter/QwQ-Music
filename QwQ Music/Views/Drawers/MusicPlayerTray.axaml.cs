using Avalonia.Controls;
using MusicPlayerTrayViewModel = QwQ_Music.ViewModels.Drawers.MusicPlayerTrayViewModel;

namespace QwQ_Music.Views.Drawers;

public partial class MusicPlayerTray : Grid
{
    public MusicPlayerTray()
    {
        InitializeComponent();
        DataContext = new MusicPlayerTrayViewModel();
    }
}
