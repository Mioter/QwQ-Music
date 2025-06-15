using Avalonia.Controls;
using QwQ_Music.ViewModels.Pages;

namespace QwQ_Music.Views.Pages;

public partial class MiaomiaoLittleToyPage : UserControl
{
    public MiaomiaoLittleToyPage()
    {
        InitializeComponent();
        DataContext = new MiaomiaoLittleToyPageViewModel();
    }
}
