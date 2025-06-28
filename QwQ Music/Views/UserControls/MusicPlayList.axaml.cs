using Avalonia.Controls;
using MusicPlayListViewModel = QwQ_Music.ViewModels.UserControls.MusicPlayListViewModel;

namespace QwQ_Music.Views.UserControls;

public partial class MusicPlayList : UserControl
{
    public MusicPlayList()
    {
        InitializeComponent();
        DataContext = MusicPlayListViewModel.Instance;
    }
}
