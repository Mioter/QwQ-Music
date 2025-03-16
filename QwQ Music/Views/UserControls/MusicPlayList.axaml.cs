using Avalonia.Controls;
using QwQ_Music.ViewModels;

namespace QwQ_Music.Views.UserControls;

public partial class MusicPlayList : UserControl
{
    public MusicPlayList()
    {
        InitializeComponent();
        DataContext = new MusicPlayListViewModel();
    }
}
