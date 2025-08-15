using Avalonia.Controls;
using QwQ_Music.ViewModels.Pages;

namespace QwQ_Music.Views.Pages;

public partial class SystemConfigPage : Grid
{
    public SystemConfigPage()
    {
        InitializeComponent();
        DataContext = new SystemConfigPageViewModel();
    }
}
