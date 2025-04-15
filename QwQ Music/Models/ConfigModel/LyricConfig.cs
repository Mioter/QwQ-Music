using System.Text.Json.Serialization;
using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace QwQ_Music.Models.ConfigModel;

public partial class LyricConfig : ObservableObject
{
    [ObservableProperty]
    public partial int Offset { get; set; }

    public DesktopLyricConfig DesktopLyric { get; set; } = new();
}

public partial class DesktopLyricConfig : ObservableObject
{
    [ObservableProperty]
    public partial bool LyricIsEnabled { get; set; }

    [ObservableProperty]
    public partial int LyricOffset { get; set; }

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
    public partial int LyricWidth { get; set; }

    [ObservableProperty]
    public partial int LyricHeight { get; set; }

    [ObservableProperty]
    public partial Color LyricMainTopColor { get; set; }

    [ObservableProperty]
    public partial Color LyricMainBottomColor { get; set; }

    [ObservableProperty]
    public partial Color LyricMainBorderColor { get; set; }

    [ObservableProperty]
    public partial Color LyricAltTopColor { get; set; }

    [ObservableProperty]
    public partial Color LyricAltBottomColor { get; set; }

    [ObservableProperty]
    public partial Color LyricAltBorderColor { get; set; }

    public Color LyricBackground { get; set; }

    // 标记原始Size属性为不序列化
    [JsonIgnore]
    public Size Size { get; set; }

    // 添加用于序列化的辅助属性
    [JsonPropertyName("Size")]
    public SizeSerializationHelper SizeHelper
    {
        get =>
            new()
            {
                Width = double.IsFinite(Size.Width) ? Size.Width : 0,
                Height = double.IsFinite(Size.Height) ? Size.Height : 0,
            };
        set => Size = new Size(value.Width, value.Height);
    }

    public int LyricMainFontSize { get; set; }

    public int LyricAltFontSize { get; set; }
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
