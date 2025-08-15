using Avalonia.Controls;
using QwQ_Music.ViewModels.Pages;

namespace QwQ_Music.Views.Pages;

public partial class AboutPage : Panel
{
    public AboutPage()
    {
        InitializeComponent();
        DataContext = new AboutPageViewModel();
    }
}
