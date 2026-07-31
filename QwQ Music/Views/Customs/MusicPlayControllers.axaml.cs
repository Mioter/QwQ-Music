using Avalonia.Controls;
using QwQ_Music.ViewModels.Drawers;

namespace QwQ_Music.Views.Customs;

public partial class MusicPlayControllers : StackPanel {
    public MusicPlayControllers() {
        InitializeComponent();
        DataContext = new MusicPlayControllersViewModel();
    }
}