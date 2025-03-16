using Avalonia.Controls;
using QwQ_Music.ViewModels;

namespace QwQ_Music.Views.Pages;

public partial class ConfigMainPage : UserControl
{
    public ConfigMainPage()
    {
        InitializeComponent();
        DataContext = new ConfigPageViewModel();
    }
}
