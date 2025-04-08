using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;

namespace QwQ_Music.ViewModels;

public partial class MusicPlayButtonViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial double PlayButtonAngle { get; set; }

    [ObservableProperty]
    public partial Thickness PlayButtonPadding { get; set; }

    public static MusicPlayerViewModel MusicPlayerViewModel { get; } = MusicPlayerViewModel.Instance;
}
