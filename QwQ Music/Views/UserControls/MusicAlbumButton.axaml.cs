using Avalonia;
using Avalonia.Controls;

namespace QwQ_Music.Views.UserControls;

public partial class MusicAlbumButton : Button
{
    public static readonly StyledProperty<bool> ExternalMouseTouchProperty = AvaloniaProperty.Register<
        MusicAlbumButton,
        bool
    >(nameof(ExternalMouseTouch));

    public static readonly StyledProperty<object> CurrentMusicItemProperty = AvaloniaProperty.Register<
        MusicAlbumButton,
        object
    >(nameof(CurrentMusicItem));

    public static readonly StyledProperty<bool> IsPlayingProperty = AvaloniaProperty.Register<MusicAlbumButton, bool>(
        nameof(IsPlaying)
    );

    public MusicAlbumButton()
    {
        InitializeComponent();
    }

    public bool ExternalMouseTouch
    {
        get => GetValue(ExternalMouseTouchProperty);
        set => SetValue(ExternalMouseTouchProperty, value);
    }

    public object CurrentMusicItem
    {
        get => GetValue(CurrentMusicItemProperty);
        set => SetValue(CurrentMusicItemProperty, value);
    }

    public bool IsPlaying
    {
        get => GetValue(IsPlayingProperty);
        set => SetValue(IsPlayingProperty, value);
    }
}
