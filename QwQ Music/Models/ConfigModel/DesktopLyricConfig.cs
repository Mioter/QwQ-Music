using System.Text.Json.Serialization;
using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace QwQ_Music.Models.ConfigModel;

public class DesktopLyricConfig : ObservableObject
{
    private bool _lyricIsEnabled;
    public bool LyricIsEnabled
    {
        get => _lyricIsEnabled;
        set => SetProperty(ref _lyricIsEnabled, value);
    }

    private int _lyricOffset;
    public int LyricOffset
    {
        get => _lyricOffset;
        set => SetProperty(ref _lyricOffset, value);
    }

    private bool _lyricIsDoubleLine;
    public bool LyricIsDoubleLine
    {
        get => _lyricIsDoubleLine;
        set => SetProperty(ref _lyricIsDoubleLine, value);
    }

    private bool _lyricIsDualLang;
    public bool LyricIsDualLang
    {
        get => _lyricIsDualLang;
        set => SetProperty(ref _lyricIsDualLang, value);
    }

    private bool _lyricIsVertical;
    public bool LyricIsVertical
    {
        get => _lyricIsVertical;
        set => SetProperty(ref _lyricIsVertical, value);
    }

    private int _lyricPositionX;
    public int LyricPositionX
    {
        get => _lyricPositionX;
        set => SetProperty(ref _lyricPositionX, value);
    }

    private int _lyricPositionY;
    public int LyricPositionY
    {
        get => _lyricPositionY;
        set => SetProperty(ref _lyricPositionY, value);
    }

    private int _lyricWidth;
    public int LyricWidth
    {
        get => _lyricWidth;
        set => SetProperty(ref _lyricWidth, value);
    }

    private int _lyricHeight;
    public int LyricHeight
    {
        get => _lyricHeight;
        set => SetProperty(ref _lyricHeight, value);
    }

    private Color _lyricMainTopColor;
    public Color LyricMainTopColor
    {
        get => _lyricMainTopColor;
        set => SetProperty(ref _lyricMainTopColor, value);
    }

    private Color _lyricMainBottomColor;
    public Color LyricMainBottomColor
    {
        get => _lyricMainBottomColor;
        set => SetProperty(ref _lyricMainBottomColor, value);
    }

    private Color _lyricMainBorderColor;
    public Color LyricMainBorderColor
    {
        get => _lyricMainBorderColor;
        set => SetProperty(ref _lyricMainBorderColor, value);
    }

    private Color _lyricAltTopColor;
    public Color LyricAltTopColor
    {
        get => _lyricAltTopColor;
        set => SetProperty(ref _lyricAltTopColor, value);
    }

    private Color _lyricAltBottomColor;
    public Color LyricAltBottomColor
    {
        get => _lyricAltBottomColor;
        set => SetProperty(ref _lyricAltBottomColor, value);
    }

    private Color _lyricAltBorderColor;
    public Color LyricAltBorderColor
    {
        get => _lyricAltBorderColor;
        set => SetProperty(ref _lyricAltBorderColor, value);
    }

    public Color LyricBackground { get; set; }

    public Size Size { get; set; }

    public int LyricMainFontSize { get; set; }

    public int LyricAltFontSize { get; set; }
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(DesktopLyricConfig))]
internal partial class DesktopLyricConfigJsonSerializerContext : JsonSerializerContext;
