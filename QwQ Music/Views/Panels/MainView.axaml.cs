using Avalonia.Controls;
using QwQ_Music.ViewModels;

namespace QwQ_Music.Views.Panels;

public partial class MainView : Grid {
    public MainView() {
        InitializeComponent();
        DataContext = new MainViewViewModel();
    }
}