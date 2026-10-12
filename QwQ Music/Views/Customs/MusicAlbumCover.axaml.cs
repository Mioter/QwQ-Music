using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media.Imaging;

namespace QwQ_Music.Views.Customs;

public class MusicAlbumCover : TemplatedControl {
    public static readonly StyledProperty<Bitmap> ThumbnailProperty =
        AvaloniaProperty.Register<MusicAlbumCover, Bitmap>(nameof(Thumbnail));

    public static readonly StyledProperty<bool> IsAutoRoundedCornersProperty =
        AvaloniaProperty.Register<MusicAlbumCover, bool>(nameof(IsAutoRoundedCorners), true);


    public Bitmap Thumbnail {
        get => GetValue(ThumbnailProperty);
        set => SetValue(ThumbnailProperty, value);
    }

    public bool IsAutoRoundedCorners {
        get => GetValue(IsAutoRoundedCornersProperty);
        set => SetValue(IsAutoRoundedCornersProperty, value);
    }
}