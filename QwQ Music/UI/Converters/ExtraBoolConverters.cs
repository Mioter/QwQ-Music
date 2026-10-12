using Avalonia.Data.Converters;

namespace QwQ_Music.UI.Converters;

public class ExtraBoolConverters {
    public static readonly IMultiValueConverter Nand = new FuncMultiValueConverter<bool, bool>(x => !x.All(y => y));

    public static readonly IMultiValueConverter Xor =
        new FuncMultiValueConverter<bool, bool>(x => x.Count(y => y) % 2 == 1);

    public static readonly IMultiValueConverter Xnor =
        new FuncMultiValueConverter<bool, bool>(x => x.Count(y => y) % 2 == 0);

    public static readonly IMultiValueConverter Nor = new FuncMultiValueConverter<bool, bool>(x => !x.Any(y => y));
}