using Avalonia.Controls;
using QwQ_Music.ViewModels.Pages;

namespace QwQ_Music.Views.Pages;

public partial class ClassificationPage : Grid {
    public ClassificationPage() {
        InitializeComponent();
        DataContext = new ClassificationPageViewModel();
    }
}