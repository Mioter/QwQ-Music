using Avalonia;
using Avalonia.Controls;
using QwQ_Music.ViewModels;
using QwQ_Music.ViewModels.Pages;
using MusicPlayListViewModel = QwQ_Music.ViewModels.UserControls.MusicPlayListViewModel;

namespace QwQ_Music.Views.UserControls;

public partial class MusicPlayList : UserControl
{
    public MusicPlayList()
    {
        InitializeComponent();
        DataContext = new MusicPlayListViewModel();
    }

    public static readonly StyledProperty<bool> IsTransparentProperty = AvaloniaProperty.Register<MusicPlayList, bool>(
        nameof(IsTransparent)
    );

    public bool IsTransparent
    {
        get => GetValue(IsTransparentProperty);
        set => SetValue(IsTransparentProperty, value);
    }
}
