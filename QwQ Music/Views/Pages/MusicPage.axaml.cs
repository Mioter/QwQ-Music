using Avalonia.Controls;
using QwQ_Music.ViewModels;

namespace QwQ_Music.Views.Pages;

public partial class MusicPage : UserControl
{
    public MusicPage()
    {
        InitializeComponent();
        DataContext = new MusicPageViewModel();
    }
}
