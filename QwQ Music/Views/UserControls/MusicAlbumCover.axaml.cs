using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;

namespace QwQ_Music.Views.UserControls;

public partial class MusicAlbumCover : UserControl
{
    public static readonly StyledProperty<Bitmap> CoverImageProperty = AvaloniaProperty.Register<
        MusicAlbumCover,
        Bitmap
    >(nameof(CoverImage));

    public MusicAlbumCover()
    {
        InitializeComponent();
    }

    public Bitmap CoverImage
    {
        get => GetValue(CoverImageProperty);
        set => SetValue(CoverImageProperty, value);
    }
}
