using Avalonia.Controls;
using QwQ_Music.ViewModels;
using InterfaceConfigPageViewModel = QwQ_Music.ViewModels.Pages.InterfaceConfigPageViewModel;

namespace QwQ_Music.Views.Pages;

public partial class InterfaceConfigPage : UserControl
{
    public InterfaceConfigPage()
    {
        InitializeComponent();
        DataContext = new InterfaceConfigPageViewModel();
    }
}
