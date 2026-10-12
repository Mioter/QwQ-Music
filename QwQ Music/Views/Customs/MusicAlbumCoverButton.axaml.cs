using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.Primitives;
using QwQ_Music.Common.Managers;

namespace QwQ_Music.Views.Customs;

public class MusicAlbumCoverButton : TemplatedControl {
    public static readonly DirectProperty<MusicAlbumCoverButton, ICommand?> CommandProperty =
        AvaloniaProperty.RegisterDirect<MusicAlbumCoverButton, ICommand?>(
            nameof(Command),
            o => o.Command,
            (o, v) => o.Command = v);

    public static readonly StyledProperty<bool> ExternalPointerOverProperty =
        AvaloniaProperty.Register<MusicAlbumCoverButton, bool>(nameof(ExternalPointerOver));

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