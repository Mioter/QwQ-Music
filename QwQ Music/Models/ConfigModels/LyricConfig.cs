using System.Collections.Frozen;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia;
using Avalonia.Layout;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using QwQ_Music.Common;
using QwQ_Music.Common.Helpers;

namespace QwQ_Music.Models.ConfigModels;

public class ColorJsonConverter : JsonConverter<Color> {
    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException();

        byte a = 255, r = 0, g = 0, b = 0;

        while (reader.Read()) {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException();

            string? propertyName = reader.GetString();
            reader.Read();

            switch (propertyName) {
                case "A":
                    a = reader.GetByte();

                    break;
                case "R":
                    r = reader.GetByte();

                    break;
                case "G":
                    g = reader.GetByte();

                    break;
                case "B":
                    b = reader.GetByte();

                    break;
            }
        }

        return new Color(a, r, g, b);
    }

    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options) {
        writer.WriteStartObject();
        writer.WriteNumber("A", value.A);
        writer.WriteNumber("R", value.R);
        writer.WriteNumber("G", value.G);
        writer.WriteNumber("B", value.B);
        writer.WriteEndObject();
    }
}

public class PixelPointJsonConverter : JsonConverter<PixelPoint> {
    public override PixelPoint Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException();

        int x = 0, y = 0;

        while (reader.Read()) {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException();

            string? propertyName = reader.GetString();
            reader.Read();

            switch (propertyName) {
                case "X":
                    x = reader.GetInt32();

                    break;
                case "Y":
                    y = reader.GetInt32();

                    break;
            }
        }

        return new PixelPoint(x, y);
    }

    public override void Write(Utf8JsonWriter writer, PixelPoint value, JsonSerializerOptions options) {
        writer.WriteStartObject();
        writer.WriteNumber("X", value.X);
        writer.WriteNumber("Y", value.Y);
        writer.WriteEndObject();
    }
}

public partial class DesktopControlConfig : ObservableObject {
    public bool IsEnabled { get; set; } = true;

    [ObservableProperty]
    public partial int TriggerDistance { get; set; } = 10;
}

public class LyricConfig : ObservableObject {
    public RolledLyricConfig RolledLyric { get; set; } = new();

    public DesktopLyricConfig DesktopLyric { get; set; } = new();

    [JsonIgnore]
    public static FrozenDictionary<string, HorizontalAlignment> TextAlignments { get; } =
        EnumHelper<HorizontalAlignment>.ToDictionary();
}

public partial class RolledLyricConfig : ObservableObject {
    [ObservableProperty]
    public partial HorizontalAlignment LyricTextAlignment { get; set; } = HorizontalAlignment.Left;

    [ObservableProperty]
    public partial bool ShowTranslation { get; set; } = true;

    [ObservableProperty]
    public partial string? RolledLyricsFont { get; set; } = AppResources.DEFAULT_FONT_KEY;

    [ObservableProperty]
    public partial double PrimaryFontSize { get; set; } = 16;

    [ObservableProperty]
    public partial double TranslationFontSize { get; set; } = 14;

    [ObservableProperty]
    public partial int LineSpacing { get; set; }

    [ObservableProperty]
    public partial int TranslationSpacing { get; set; } = 5;
}

public partial class DesktopLyricConfig : ObservableObject {
    [ObservableProperty]
    public partial bool IsTopmost { get; set; } = true;

    [ObservableProperty]
    public partial bool IsAutoFade { get; set; } = true;

    public int FadeInMilliseconds { get; set; } = 500;

    public int FadeOutMilliseconds { get; set; } = 5000;

    public int FadeOutDelayMilliseconds { get; set; } = 10000;

    public int MinimumOpacity { get; set; }

    [ObservableProperty]
    public partial TimeSpan CrossFadeTime { get; set; } = TimeSpan.FromMilliseconds(500);

    public bool IsEnabled { get; set; } = true;

    public bool IsAnchored { get; set; }

    [ObservableProperty]
    public partial bool IsDoubleLine { get; set; }

    [ObservableProperty]
    public partial bool IsDualLang { get; set; } = true;

    [ObservableProperty]
    public partial string Font { get; set; } = AppResources.DEFAULT_FONT_KEY;

    [ObservableProperty]
    [JsonConverter(typeof(PixelPointJsonConverter))]
    public partial PixelPoint Position { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Margin))]
    public partial double Spacing { get; set; } = 10;

    [ObservableProperty]
    public partial double Width { get; set; } = 800;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowCornerRadius))]
    public partial double CornerRadius { get; set; }

    public CornerRadius WindowCornerRadius => new(CornerRadius);

    public Thickness Margin => new(Spacing);

    [ObservableProperty]
    public partial HorizontalAlignment TextAlignment { get; set; } = HorizontalAlignment.Center;

    [ObservableProperty]
    public partial FontWeight PrimaryWeight { get; set; } = FontWeight.Bold;

    [ObservableProperty]
    public partial FontWeight SecondaryWeight { get; set; } = FontWeight.Bold;

    [ObservableProperty]
    public partial FontWeight AltPrimaryWeight { get; set; } = FontWeight.Normal;

    [ObservableProperty]
    public partial FontWeight AltSecondaryWeight { get; set; } = FontWeight.Normal;

    [JsonConverter(typeof(ColorJsonConverter))]
    [ObservableProperty]
    public partial Color MainTopColor { get; set; } = Color.FromArgb(255, 255, 35, 112);

    [JsonConverter(typeof(ColorJsonConverter))]
    [ObservableProperty]
    public partial Color MainBottomColor { get; set; } = Color.FromArgb(255, 180, 152, 255);

    [JsonConverter(typeof(ColorJsonConverter))]
    [ObservableProperty]
    public partial Color MainBorderColor { get; set; } = Colors.White;

    [JsonConverter(typeof(ColorJsonConverter))]
    [ObservableProperty]
    public partial Color AltTopColor { get; set; } = Color.FromArgb(255, 122, 68, 255);

    [JsonConverter(typeof(ColorJsonConverter))]
    [ObservableProperty]
    public partial Color AltBottomColor { get; set; } = Color.FromArgb(255, 255, 134, 227);

    [JsonConverter(typeof(ColorJsonConverter))]
    [ObservableProperty]
    public partial Color AltBorderColor { get; set; } = Colors.White;

    [ObservableProperty]
    [JsonConverter(typeof(ColorJsonConverter))]
    public partial Color Background { get; set; }

    [ObservableProperty]
    public partial double MainFontSize { get; set; } = 20;

    [ObservableProperty]
    public partial double AltFontSize { get; set; } = 18;

    [ObservableProperty]
    public partial double MainCharSpacing { get; set; } = 2;

    [ObservableProperty]
    public partial double AltCharSpacing { get; set; } = 2;

    [ObservableProperty]
    public partial double MainStrokeThickness { get; set; } = 3;

    [ObservableProperty]
    public partial double AltStrokeThickness { get; set; } = 3;

    [ObservableProperty]
    public partial double MainTranslateSpacing { get; set; } = 2;

    [ObservableProperty]
    public partial double AltTranslateSping { get; set; } = 2;
}