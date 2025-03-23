using System.Text.Json.Serialization;
using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace QwQ_Music.Models.ConfigModel;

public class DesktopLyricConfig : ObservableObject
{
    public bool LyricIsEnabled
    {
        get;
        set => SetProperty(ref field, value);
    }

    public int LyricOffset
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool LyricIsDoubleLine
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool LyricIsDualLang
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool LyricIsVertical
    {
        get;
        set => SetProperty(ref field, value);
    }

    public int LyricPositionX
    {
        get;
        set => SetProperty(ref field, value);
    }

    public int LyricPositionY
    {
        get;
        set => SetProperty(ref field, value);
    }

    public int LyricWidth
    {
        get;
        set => SetProperty(ref field, value);
    }

    public int LyricHeight
    {
        get;
        set => SetProperty(ref field, value);
    }

    public Color LyricMainTopColor
    {
        get;
        set => SetProperty(ref field, value);
    }

    public Color LyricMainBottomColor
    {
        get;
        set => SetProperty(ref field, value);
    }

    public Color LyricMainBorderColor
    {
        get;
        set => SetProperty(ref field, value);
    }

    public Color LyricAltTopColor
    {
        get;
        set => SetProperty(ref field, value);
    }

    public Color LyricAltBottomColor
    {
        get;
        set => SetProperty(ref field, value);
    }

    public Color LyricAltBorderColor
    {
        get;
        set => SetProperty(ref field, value);
    }

    public Color LyricBackground { get; set; }

    public Size Size { get; set; }

    public int LyricMainFontSize { get; set; }

    public int LyricAltFontSize { get; set; }
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(DesktopLyricConfig))]
internal partial class DesktopLyricConfigJsonSerializerContext : JsonSerializerContext;
