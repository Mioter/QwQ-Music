using Avalonia.Controls;
using QwQ_Music.ViewModels;
using QwQ_Music.ViewModels.Pages;

namespace QwQ_Music.Views.Pages;

public partial class ClassificationPage : UserControl
{
    public ClassificationPage()
    {
        InitializeComponent();
        DataContext = new ClassificationPageViewModel();
    }
}
