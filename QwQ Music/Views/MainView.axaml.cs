using Avalonia.Controls;
using QwQ_Music.ViewModels;

namespace QwQ_Music.Views;

public partial class MainView : Grid
{
    public MainView()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
