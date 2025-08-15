using Avalonia.Controls;
using QwQ_Music.ViewModels.Pages;

namespace QwQ_Music.Views.Pages;

public partial class UiConfigPage : Grid
{
    public UiConfigPage()
    {
        InitializeComponent();
        DataContext = new UiConfigPageViewModel();
    }
}
