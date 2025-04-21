using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using QwQ_Music.ViewModels;

namespace QwQ_Music.Views.UserControls;

public partial class MusicAlbumCover : UserControl
{
    public MusicAlbumCover()
    {
        InitializeComponent();
    }
    

    public static readonly StyledProperty<Bitmap> CoverImageProperty = AvaloniaProperty.Register<MusicAlbumCover, Bitmap>(
        nameof(CoverImage));

    public Bitmap CoverImage
    {
        get => GetValue(CoverImageProperty);
        set => SetValue(CoverImageProperty, value);
    }
    
}

