using Avalonia.Controls;
using QwQ_Music.ViewModels.Pages;

namespace QwQ_Music.Views.Pages;

public partial class MusicListClassPage : Panel
{
    public MusicListClassPage()
    {
        InitializeComponent();
        DataContext = new MusicListClassPageViewModel();
    }
}
