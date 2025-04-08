using Avalonia.Controls;
using QwQ_Music.ViewModels;

namespace QwQ_Music.Views.Pages;

public partial class InterfaceConfigPage : UserControl
{
    public InterfaceConfigPage()
    {
        InitializeComponent();
        DataContext = new InterfaceConfigPageViewModel();
    }
}
