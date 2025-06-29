using Avalonia.Controls;
using QwQ_Music.ViewModels.Pages;

namespace QwQ_Music.Views.Pages;

public partial class MusicListsPage : UserControl
{
    public MusicListsPage()
    {
        InitializeComponent();
        DataContext = new MusicListsPageViewModel();
    }
}
