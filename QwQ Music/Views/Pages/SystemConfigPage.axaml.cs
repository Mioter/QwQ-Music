using Avalonia.Controls;
using QwQ_Music.ViewModels.Pages;

namespace QwQ_Music.Views.Pages;

public partial class SystemConfigPage : UserControl
{
    public SystemConfigPage()
    {
        InitializeComponent();
        DataContext = new SystemConfigPageViewModel();
    }
}
