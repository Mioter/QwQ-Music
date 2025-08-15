using Avalonia.Controls;
using MusicCoverPageViewModel = QwQ_Music.ViewModels.Drawers.MusicCoverPageViewModel;

namespace QwQ_Music.Views.Drawers;

public partial class MusicPlayerPanel : Grid
{
    public MusicPlayerPanel()
    {
        InitializeComponent();
        DataContext = new MusicCoverPageViewModel();
    }
}
