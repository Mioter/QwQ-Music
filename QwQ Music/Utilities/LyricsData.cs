using System.Collections.Generic;

namespace QwQ_Music.Utilities;

public class LyricsData
{
    // 歌词元数据
    public string? Title { get; set; }
    public string? Artist { get; set; }
    public string? Album { get; set; }
    public string? Creator { get; set; }
    public double Offset { get; set; }

    // 主要歌词（原文）
    public Dictionary<double, string> PrimaryLyrics { get; set; } = new() { { 0, "暂无歌词" } };

    // 翻译歌词（如果有）
    public Dictionary<double, string>? TranslationLyrics { get; set; }

    // 判断是否有翻译
    public bool HasTranslation => TranslationLyrics is { Count: > 0 };
}
