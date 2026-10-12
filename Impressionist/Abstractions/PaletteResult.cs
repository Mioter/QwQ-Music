using System.Collections.Generic;
using System.Numerics;

namespace Impressionist.Abstractions;

public class PaletteResult
{
    internal PaletteResult(List<Vector3> palette, bool paletteIsDark, ThemeColorResult themeColor)
    {
        Palette = palette;
        PaletteIsDark = paletteIsDark;
        ThemeColor = themeColor;
    }

    public List<Vector3> Palette { get; }

    public bool PaletteIsDark { get; }

    public ThemeColorResult ThemeColor { get; }
}

public class ThemeColorResult
{
    internal ThemeColorResult(Vector3 color, bool colorIsDark)
    {
        Color = color;
        ColorIsDark = colorIsDark;
    }

    public Vector3 Color { get; }

    public bool ColorIsDark { get; }
}
