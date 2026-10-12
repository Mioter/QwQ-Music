using Avalonia.Controls;
using QwQ_Music.ViewModels.Pages;

namespace QwQ_Music.Views.Pages;

public partial class PlayerConfigPage : UserControl {
    public PlayerConfigPage() {
        InitializeComponent();
        DataContext = new PlayConfigPageViewModel();
    }
}