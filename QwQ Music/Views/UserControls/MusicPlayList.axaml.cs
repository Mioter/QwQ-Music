using Avalonia;
using Avalonia.Controls;
using QwQ_Music.ViewModels;

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
