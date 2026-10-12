using Avalonia.Controls;
using QwQ_Music.ViewModels.Drawers;

namespace QwQ_Music.Views.Drawers;

public partial class MusicPlaylist : UserControl {
    public MusicPlaylist() {
        InitializeComponent();
        DataContext = new MusicPlaylistViewModel();
    }
}