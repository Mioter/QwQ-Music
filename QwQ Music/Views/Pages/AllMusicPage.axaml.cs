using Avalonia.Controls;
using QwQ_Music.ViewModels;

namespace QwQ_Music.Views.Pages;

public partial class AllMusicPage : UserControl
{
    public AllMusicPage()
    {
        InitializeComponent();
        DataContext = new AllMusicPageViewModel();
    }
}
