using Avalonia.Controls;
using QwQ_Music.ViewModels.Pages;

namespace QwQ_Music.Views.Pages;

public partial class AboutPage : UserControl
{
    private readonly AboutPageViewModel _aboutPageViewModel = new();

    public AboutPage()
    {
        InitializeComponent();
        DataContext = _aboutPageViewModel;
    }
}
