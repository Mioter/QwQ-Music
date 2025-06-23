using Avalonia.Controls;
using QwQ_Music.ViewModels.Pages;

namespace QwQ_Music.Views.Pages;

public partial class AlbumClassPage : UserControl
{
    public AlbumClassPage()
    {
        InitializeComponent();
        DataContext = new AlbumClassPageViewModel();
    }
}
