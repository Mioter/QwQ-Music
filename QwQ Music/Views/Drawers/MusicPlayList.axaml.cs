using Avalonia.Controls;
using MusicPlayListViewModel = QwQ_Music.ViewModels.Drawers.MusicPlayListViewModel;

namespace QwQ_Music.Views.Drawers;

public partial class MusicPlayList : Grid
{
    public MusicPlayList()
    {
        InitializeComponent();
        DataContext = new MusicPlayListViewModel();
    }
}
