using Avalonia.Controls;
using QwQ_Music.ViewModels;
using MusicListsPageViewModel = QwQ_Music.ViewModels.Pages.MusicListsPageViewModel;

namespace QwQ_Music.Views.Pages;

public partial class MusicListsPage : UserControl
{
    public MusicListsPage()
    {
        InitializeComponent();
        DataContext = new MusicListsPageViewModel();
    }
}
