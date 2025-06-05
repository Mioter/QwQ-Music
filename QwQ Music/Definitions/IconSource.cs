using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace QwQ_Music.Definitions;

public record IconSource { }

public record GeometryIconSource(StreamGeometry Geometry) : IconSource;

public record BitmapIconSource(Bitmap Bitmap) : IconSource;
