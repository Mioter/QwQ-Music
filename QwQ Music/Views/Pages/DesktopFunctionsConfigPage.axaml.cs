using Avalonia.Controls;
using QwQ_Music.ViewModels.Pages;

namespace QwQ_Music.Views.Pages;

public partial class DesktopFunctionsConfigPage : Grid {
    public DesktopFunctionsConfigPage() {
        InitializeComponent();
        DataContext = new DesktopFunctionsConfigPageViewModel();
    }
}