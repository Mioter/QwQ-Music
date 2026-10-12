using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.Primitives;
using QwQ_Music.Common.Managers;

namespace QwQ_Music.Views.Customs;

public class PlaylistAlbumCoverButton : TemplatedControl {
    public static readonly DirectProperty<PlaylistAlbumCoverButton, ICommand?> CommandProperty =
        AvaloniaProperty.RegisterDirect<PlaylistAlbumCoverButton, ICommand?>(
            nameof(Command),
            o => o.Command,
            (o, v) => o.Command = v);

    public static readonly StyledProperty<bool> ExternalPointerOverProperty =
        AvaloniaProperty.Register<PlaylistAlbumCoverButton, bool>(nameof(ExternalPointerOver));

    public ICommand? Command {
        get;
        set => SetAndRaise(CommandProperty, ref field, value);
    }

    public bool ExternalPointerOver {
        get => GetValue(ExternalPointerOverProperty);
        set => SetValue(ExternalPointerOverProperty, value);
    }

    public static AudioPlayManager AudioPlayManager => AudioPlayManager.Instance;
}