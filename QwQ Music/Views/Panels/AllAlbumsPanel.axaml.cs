using Avalonia.Controls;
using QwQ_Music.ViewModels.Panels;

namespace QwQ_Music.Views.Panels;

public partial class AllAlbumsPanel : UserControl {
    public AllAlbumsPanel() {
        InitializeComponent();
        DataContext = new AllAlbumsPanelViewModel();
    }
}