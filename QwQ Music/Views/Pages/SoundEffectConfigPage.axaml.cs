using Avalonia.Controls;
using QwQ_Music.ViewModels.Pages;

namespace QwQ_Music.Views.Pages;

public partial class SoundEffectConfigPage : Grid {
    public SoundEffectConfigPage() {
        InitializeComponent();
        DataContext = new SoundEffectConfigViewModel();
    }
}