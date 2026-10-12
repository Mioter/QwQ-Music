using Avalonia.Markup.Xaml;

namespace QwQ_Music.MarkupExtensions;

public class EnumValuesBinding(Type enumType) : MarkupExtension {
    public override object ProvideValue(IServiceProvider serviceProvider) {
        if (enumType is not { IsEnum: true }) {
            throw new InvalidCastException();
        }

        return Converter(enumType).ToDictionary(t => t.Item1, t => t.Item2);

        IEnumerable<(string, object)> Converter(Type t) {
            foreach (object v in Enum.GetValues(t)) {
                yield return (Enum.GetName(t, v)!, v);
            }
        }
    }
}