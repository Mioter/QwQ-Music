using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using QwQ_Music.ViewModels.Bases;

namespace QwQ_Music.ViewModels.UserControls;

public partial class MusicPlayButtonViewModel : ViewModelBase
{
    [ObservableProperty] public partial double PlayButtonAngle { get; set; }

    [ObservableProperty] public partial Thickness PlayButtonPadding { get; set; }

    public static MusicPlayerViewModel MusicPlayerViewModel => MusicPlayerViewModel.Default;
}
