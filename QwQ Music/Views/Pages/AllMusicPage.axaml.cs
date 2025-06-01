using Avalonia.Controls;
using QwQ_Music.ViewModels;
using AllMusicPageViewModel = QwQ_Music.ViewModels.Pages.AllMusicPageViewModel;

namespace QwQ_Music.Views.Pages;

public partial class AllMusicPage : UserControl
{
    public AllMusicPage()
    {
        InitializeComponent();
        DataContext = new AllMusicPageViewModel();
    }
}
