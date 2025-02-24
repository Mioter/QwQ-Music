using Avalonia.Controls;
using QwQ_Music.ViewModels;

namespace QwQ_Music.Pages;

public partial class ClassifiedPage : UserControl
{
    public ClassifiedPage()
    {
        InitializeComponent();
        DataContext = new ClassifiedPageViewModel();
    }
}
