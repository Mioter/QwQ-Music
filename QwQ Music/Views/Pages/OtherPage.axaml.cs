using Avalonia.Controls;
using QwQ_Music.ViewModels.Pages;

namespace QwQ_Music.Views.Pages;

public partial class OtherPage : Grid {
    public OtherPage() {
        InitializeComponent();
        DataContext = new OtherPageViewModel();
    }
}