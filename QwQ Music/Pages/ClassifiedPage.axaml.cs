using Avalonia.Controls;
using QwQ_Music.ViewModels;

namespace QwQ_Music.Pages;

public partial class ClassificationPage : UserControl
{
    public ClassificationPage()
    {
        InitializeComponent();
        DataContext = new ClassificationPageViewModel();
    }
}
