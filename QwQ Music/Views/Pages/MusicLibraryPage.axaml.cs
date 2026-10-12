using Avalonia.Controls;
using QwQ_Music.ViewModels.Pages;

namespace QwQ_Music.Views.Pages;

public partial class MusicLibraryPage : UserControl {
    public MusicLibraryPage() {
        InitializeComponent();
        DataContext = new MusicLibraryPageViewModel();
    }
}