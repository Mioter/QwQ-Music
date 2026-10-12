using Avalonia.Controls;
using QwQ_Music.ViewModels.Pages;

namespace QwQ_Music.Views.Pages;

public partial class ThemeConfigPage : Grid {
    public ThemeConfigPage() {
        InitializeComponent();
        DataContext = new UiConfigPageViewModel();
    }
}