using Avalonia.Controls;
using QwQ_Music.ViewModels.Pages;

namespace QwQ_Music.Views.Pages;

public partial class JoyPage : UserControl {
    public JoyPage() {
        InitializeComponent();
        DataContext = new JoyViewModel();
    }
}