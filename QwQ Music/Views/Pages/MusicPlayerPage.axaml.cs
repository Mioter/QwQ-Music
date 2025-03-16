using Avalonia.Controls;
using QwQ_Music.ViewModels;

namespace QwQ_Music.Views.Pages;

public partial class MusicPlayerPage : UserControl
{
    public MusicPlayerPage()
    {
        InitializeComponent();
        DataContext = new MusicPlayerPageViewModel();
    }
}
