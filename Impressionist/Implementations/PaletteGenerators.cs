namespace Impressionist.Implementations;

public static class PaletteGenerators
{
    public static readonly KMeansPaletteGenerator KMeansPaletteGenerator = new();
    public static readonly OctTreePaletteGenerator OctTreePaletteGenerator = new();
}
