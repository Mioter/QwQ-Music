using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia;
using Avalonia.Layout;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using QwQ_Music.Common;

namespace QwQ_Music.Models.ConfigModels;

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

            string? propertyName = reader.GetString();
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

public class PixelPointJsonConverter : JsonConverter<PixelPoint>
{
    public override PixelPoint Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        int x = 0,
            y = 0;

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

            string? propertyName = reader.GetString();
            reader.Read();

            switch (propertyName)
            {
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

    public override void Write(Utf8JsonWriter writer, PixelPoint value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("X", value.X);
        writer.WriteNumber("Y", value.Y);
        writer.WriteEndObject();
    }
}

public class LyricConfig : ObservableObject
{
    public RolledLyricConfig RolledLyric { get; set; } = new();

    public DesktopLyricConfig DesktopLyric { get; set; } = new();

    // ReSharper disable once CollectionNeverQueried.Global
    public static Dictionary<HorizontalAlignment, string> TextAlignments { get; } = new()
    {
        [HorizontalAlignment.Left] = "左对齐",
        [HorizontalAlignment.Center] = "居中",
        [HorizontalAlignment.Right] = "右对齐",
    };
}

public partial class RolledLyricConfig : ObservableObject
{
    [ObservableProperty] public partial HorizontalAlignment LyricTextAlignment { get; set; } = HorizontalAlignment.Left;

    [ObservableProperty] public partial bool ShowTranslation { get; set; }

    [ObservableProperty] public partial string? RolledLyricsFont { get; set; } = AppResources.DEFAULT_FONT_KEY;
    
    [ObservableProperty] public partial double LyricFontSize { get; set; } = 15;
    
    [ObservableProperty] public partial double CurrentLyricFontSize { get; set; } = 15;
        
    [ObservableProperty] public partial int TranslationSpacing { get; set; } = 5;
}

public partial class DesktopLyricConfig : ObservableObject
{
    public bool LyricIsEnabled { get; set; }

    public bool LockLyricWindow { get; set; }

    [ObservableProperty] public partial bool LyricIsDoubleLine { get; set; } = true;

    [ObservableProperty] public partial bool LyricIsDualLang { get; set; }

    [ObservableProperty] public partial bool LyricIsVertical { get; set; }

    [ObservableProperty] public partial string DesktopLyricsFont { get; set; } = AppResources.DEFAULT_FONT_KEY;

    [ObservableProperty]
    [JsonConverter(typeof(PixelPointJsonConverter))]
    public partial PixelPoint Position { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LyricMargin))]
    public partial double LyricSpacing { get; set; } = 10;

    [ObservableProperty] public partial double LyricWidth { get; set; } = 500;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowCornerRadius))]
    public partial double CornerRadius { get; set; }

    public CornerRadius WindowCornerRadius => new(CornerRadius);

    public Thickness LyricMargin => new(LyricSpacing);

    [ObservableProperty] public partial HorizontalAlignment LyricTextAlignment { get; set; } = HorizontalAlignment.Center;

    [JsonConverter(typeof(ColorJsonConverter))]
    [ObservableProperty]
    public partial Color LyricMainTopColor { get; set; } = Color.FromArgb(255, 255, 35, 112);

    [JsonConverter(typeof(ColorJsonConverter))]
    [ObservableProperty]
    public partial Color LyricMainBottomColor { get; set; } = Color.FromArgb(255, 180, 152, 255);

    [JsonConverter(typeof(ColorJsonConverter))]
    [ObservableProperty]
    public partial Color LyricMainBorderColor { get; set; } = Colors.White;

    [JsonConverter(typeof(ColorJsonConverter))]
    [ObservableProperty]
    public partial Color LyricAltTopColor { get; set; } = Color.FromArgb(255, 122, 68, 255);

    [JsonConverter(typeof(ColorJsonConverter))]
    [ObservableProperty]
    public partial Color LyricAltBottomColor { get; set; } = Color.FromArgb(255, 255, 134, 227);

    [JsonConverter(typeof(ColorJsonConverter))]
    [ObservableProperty]
    public partial Color LyricAltBorderColor { get; set; } = Colors.White;

    [ObservableProperty]
    [JsonConverter(typeof(ColorJsonConverter))]
    public partial Color LyricBackground { get; set; }

    [ObservableProperty] public partial double LyricMainFontSize { get; set; } = 20;

    [ObservableProperty] public partial double LyricAltFontSize { get; set; } = 18;

    [ObservableProperty] public partial double LyricMainLetterSpacing { get; set; } = 2;

    [ObservableProperty] public partial double LyricAltLetterSpacing { get; set; } = 2;

    [ObservableProperty] public partial double LyricMainStrokeThickness { get; set; } = 3;

    [ObservableProperty] public partial double LyricAltStrokeThickness { get; set; } = 3;

    [ObservableProperty] public partial double LyricMainTranslateSpacing { get; set; } = 2;

    [ObservableProperty] public partial double LyricAltTranslateSping { get; set; } = 2;
}
