using Avalonia.Controls;
using QwQ_Music.ViewModels;
using MusicCoverPageViewModel = QwQ_Music.ViewModels.Pages.MusicCoverPageViewModel;

namespace QwQ_Music.Views.Pages;

public partial class MusicCoverPage : UserControl
{
    public MusicCoverPage()
    {
        InitializeComponent();
        DataContext = new MusicCoverPageViewModel();
    }
}
