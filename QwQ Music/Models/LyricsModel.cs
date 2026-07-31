using CommunityToolkit.Mvvm.ComponentModel;
using QwQ_Music.Common.Services;

namespace QwQ_Music.Models;

/// 歌词行结构体
public record struct LyricLine(double TimePoint, string Primary, string? Secondary = null) {
    public static readonly LyricLine Empty = new(0, string.Empty);
}

public readonly record struct LyricLinePair {
    public required LyricLine Primary { get; init; }
    public required LyricLine Alternate { get; init; }
};

public partial class LyricsModel : ObservableObject {
    public delegate void LyricLineChangedEventHandler(object sender, LyricLinePair newLyric);

    public double Offset {
        get => Lyrics.Offset;
        set => Lyrics.Offset = value;
    }

    [ObservableProperty]
    public partial int CurrentIndex { get; set; }

    [ObservableProperty]
    public partial LyricLinePair Current { get; private set; }

    [ObservableProperty]
    public partial LyricsData Lyrics { get; set; } = LyricsData.Loading;

    /// <summary>
    ///     获取歌词总数
    /// </summary>
    public int Total => Lyrics.Data.Count;

    public event LyricLineChangedEventHandler? LyricLineChanged;

    /// <summary>
    ///     获取当前歌词到下一句歌词的时间间隔
    /// </summary>
    /// <param name="currPos">当前播放时间（秒）</param>
    /// <returns>到下一句歌词的时间间隔（秒），如果没有下一句则返回-1</returns>
    public double GetNextLyricsInterval(double currPos) {
        if (Total == 0 || Total - 1 == CurrentIndex)
            return -1;
        // 计算到下一句歌词的时间间隔（考虑偏移量）
        return Lyrics[CurrentIndex + 1].TimePoint - currPos;
    }

    public void UpdateLyricsIndex(double currPos) {
        if (Lyrics.Data.Count == 0)
            return;
        int newIndex = Math.Max(0, Lyrics.Data.FindLastIndex(line => line.TimePoint <= currPos));

        CurrentIndex = newIndex;
        Current = new LyricLinePair {
            Primary = Lyrics[CurrentIndex],
            Alternate = CurrentIndex + 1 == Total ? LyricLine.Empty : Lyrics.Data[CurrentIndex + 1]
        };

        // 触发歌词变更事件，同时传递当前歌词和下一句歌词
        LyricLineChanged?.Invoke(this, Current);
    }

    public void Reset(LyricsData? newValue = null) {
        Offset = 0;
        CurrentIndex = 0;
        Current = new LyricLinePair { Primary = Lyrics[0], Alternate = Total > 1 ? Lyrics[1] : Lyrics[0] };
        if (newValue != null)
            Lyrics = newValue;
    }
}

public sealed class LyricsData {
    private LyricsData() { }

    public static readonly LyricsData Default = new() {
        Title = null, Artist = null, Album = null, Data = [new LyricLine(0, "QwQ Music")]
    };

    public static readonly LyricsData Loading = new() {
        Title = null,
        Artist = null,
        Album = null,
        Data = [new LyricLine(0, I18NService.Lang.Translation["Loading lyrics..."])]
    };

    private LyricLine DefaultLyricLine => new(0, $"{Title} - {Artist}", Album);

    public LyricLine this[int index] => index < Data.Count && index > 0 ? Data[index] : DefaultLyricLine;

    // 歌词元数据
    public required string? Title { get; init; }

    public required string? Artist { get; init; }

    public required string? Album { get; init; }

    public string? Creator { get; init; }

    public double Offset {
        get;
        set {
            Data.AsParallel().ForAll(line => line.TimePoint += value - field);
            field = value;
            LoggerService.Debug($"更新音频偏移量：{field} -> {value}");
        }
    }

    // public double DesktopLyricAdditionalOffset { get; set; } =
    //     -ConfigManager.LyricConfig.DesktopLyric.CrossFadeMilliseconds;

    public List<LyricLine> Data {
        get;
        private init {
            field = value;
            field.AsParallel().ForAll(line => line.TimePoint += Offset);
        }
    } = [];

    public static LyricsData Create(string? title, string? artist, string? album, string? creator = null) {
        return new LyricsData {
            Title = title,
            Artist = artist,
            Album = album,
            Creator = creator,
            Data = [new LyricLine(0, $"{title} - {artist}", album)]
        };
    }
}