using Avalonia.Controls;
using QwQ_Music.ViewModels.Pages;

namespace QwQ_Music.Views.Pages;

public partial class AlbumClassPage : Panel
{
    public AlbumClassPage()
    {
        InitializeComponent();
        DataContext = new AlbumClassPageViewModel();
    }
}
