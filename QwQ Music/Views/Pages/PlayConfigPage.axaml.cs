using Avalonia.Controls;
using PlayConfigPageViewModel = QwQ_Music.ViewModels.Pages.PlayConfigPageViewModel;

namespace QwQ_Music.Views.Pages;

public partial class PlayConfigPage : UserControl
{
    public PlayConfigPage()
    {
        InitializeComponent();
        DataContext = new PlayConfigPageViewModel();
    }
}
