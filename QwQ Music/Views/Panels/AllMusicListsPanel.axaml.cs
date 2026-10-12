using Avalonia.Controls;
using QwQ_Music.ViewModels.Panels;

namespace QwQ_Music.Views.Panels;

public partial class AllMusicListsPanel : UserControl {
    public AllMusicListsPanel() {
        InitializeComponent();
        DataContext = new AllMusicListsPanelViewModel();
    }
}