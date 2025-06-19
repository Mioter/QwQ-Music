using Avalonia.Controls;
using QwQ_Music.ViewModels.Pages;

namespace QwQ_Music.Views.Pages;

public partial class HotkeyConfigPage : UserControl
{
    public HotkeyConfigPage()
    {
        InitializeComponent();
        DataContext = new HotkeyConfigPageViewModel();
    }
}
