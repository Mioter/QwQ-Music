using Avalonia.Controls;
using QwQ_Music.ViewModels;

namespace QwQ_Music.Pages;

public partial class ConfigMainPage : UserControl
{
    public ConfigMainPage()
    {
        InitializeComponent();
        DataContext = new ConfigPageViewModel();
    }
}
