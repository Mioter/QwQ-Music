using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia;
using Avalonia.Layout;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace QwQ_Music.Models.ConfigModel;

public class ColorJsonConverter : JsonConverter<Color>
{
    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        byte a = 255,
            r = 0,
            g = 0,
            b = 0;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }

            string propertyName = reader.GetString()!;
            reader.Read();

            switch (propertyName)
            {
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

    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("A", value.A);
        writer.WriteNumber("R", value.R);
        writer.WriteNumber("G", value.G);
        writer.WriteNumber("B", value.B);
        writer.WriteEndObject();
    }
}

public class HsvColorJsonConverter : JsonConverter<HsvColor>
{
    public override HsvColor Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        double h = 0,
            s = 0,
            v = 0,
            a = 0;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }

            string propertyName = reader.GetString()!;
            reader.Read();

            switch (propertyName)
            {
                case "A":
                    a = reader.GetDouble();
                    break;
                case "H":
                    h = reader.GetDouble();
                    break;
                case "S":
                    s = reader.GetDouble();
                    break;
                case "V":
                    v = reader.GetDouble();
                    break;
            }
        }

        return new HsvColor(a, h, s, v);
    }

    public override void Write(Utf8JsonWriter writer, HsvColor value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("A", value.A);
        writer.WriteNumber("H", value.H);
        writer.WriteNumber("S", value.S);
        writer.WriteNumber("V", value.V);
        writer.WriteEndObject();
    }
}

public class LyricConfig : ObservableObject
{
    public RolledLyricConfig RolledLyri { get; set; } = new();

    public DesktopLyricConfig DesktopLyric { get; set; } = new();

    public bool IsExpandedRolledLyricConfig { get; set; }

    public bool IsExpandedDesktopLyricConfig { get; set; }

    [JsonIgnore]
    public static HorizontalAlignment[] TextAlignments { get; } =
        [HorizontalAlignment.Left, HorizontalAlignment.Center, HorizontalAlignment.Right];
}

public partial class RolledLyricConfig : ObservableObject
{
    [ObservableProperty]
    public partial HorizontalAlignment LyricTextAlignment { get; set; } = HorizontalAlignment.Left;

    [ObservableProperty]
    public partial bool ShowTranslation { get; set; }
}

public partial class DesktopLyricConfig : ObservableObject
{
    public bool LyricIsEnabled { get; set; }

    public bool LockLyricWindow { get; set; }

    [ObservableProperty]
    public partial bool LyricIsDoubleLine { get; set; }

    [ObservableProperty]
    public partial bool LyricIsDualLang { get; set; }

    [ObservableProperty]
    public partial bool LyricIsVertical { get; set; }

    [ObservableProperty]
    public partial int LyricPositionX { get; set; }

    [ObservableProperty]
    public partial int LyricPositionY { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LyricMargin))]
    public partial double LyricSpacing { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LyricWidowWidth))]
    public partial double LyricWidth { get; set; } = 500;

    public double LyricWidowWidth => LyricWidth + LyricSpacing * 2;

    public Thickness LyricMargin => new(0, LyricSpacing);

    [ObservableProperty]
    public partial HorizontalAlignment LyricTextAlignment { get; set; } = HorizontalAlignment.Center;

    [JsonConverter(typeof(ColorJsonConverter))]
    [ObservableProperty]
    public partial Color LyricMainTopColor { get; set; }

    [JsonConverter(typeof(ColorJsonConverter))]
    [ObservableProperty]
    public partial Color LyricMainBottomColor { get; set; }

    [JsonConverter(typeof(ColorJsonConverter))]
    [ObservableProperty]
    public partial Color LyricMainBorderColor { get; set; }

    [JsonConverter(typeof(ColorJsonConverter))]
    [ObservableProperty]
    public partial Color LyricAltTopColor { get; set; }

    [JsonConverter(typeof(ColorJsonConverter))]
    [ObservableProperty]
    public partial Color LyricAltBottomColor { get; set; }

    [JsonConverter(typeof(ColorJsonConverter))]
    [ObservableProperty]
    public partial Color LyricAltBorderColor { get; set; }

    [ObservableProperty]
    [JsonConverter(typeof(HsvColorJsonConverter))]
    public partial HsvColor LyricBackground { get; set; }

    [ObservableProperty]
    public partial int LyricMainFontSize { get; set; } = 15;

    [ObservableProperty]
    public partial int LyricAltFontSize { get; set; } = 12;
}

// 用于序列化的辅助类
public class SizeSerializationHelper
{
    public double Width { get; set; }
    public double Height { get; set; }
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(LyricConfig))]
[JsonSerializable(typeof(SizeSerializationHelper))]
internal partial class LyricConfigJsonSerializerContext : JsonSerializerContext;
