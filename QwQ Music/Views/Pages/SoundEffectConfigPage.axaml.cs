using Avalonia.Controls;
using QwQ_Music.ViewModels;

namespace QwQ_Music.Views.Pages;

public partial class SoundEffectConfigPage : UserControl
{
    public SoundEffectConfigPage()
    {
        InitializeComponent();
        DataContext = new SoundEffectConfigViewModel();
    }
}
