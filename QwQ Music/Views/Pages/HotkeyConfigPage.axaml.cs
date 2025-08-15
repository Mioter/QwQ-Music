using Avalonia.Controls;
using QwQ_Music.ViewModels.Pages;

namespace QwQ_Music.Views.Pages;

public partial class HotkeyConfigPage : StackPanel
{
    public HotkeyConfigPage()
    {
        InitializeComponent();
        DataContext = new HotkeyConfigPageViewModel();
    }
}
