using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace QwQ_Music.Models;

/// 歌词行结构体
public record struct LyricLine(double TimePoint, string Primary, string? Translation = null);

public partial class LyricsModel(LyricsData lyricsData) : ObservableObject
{
    public delegate void CurrentLyricsChangedEventHandler(object sender, int lyricsIndex, LyricLine lyricsLine);

    public event CurrentLyricsChangedEventHandler? CurrentLyrics;

    private readonly List<double> _timePoints =
        lyricsData.Lyrics.Count > 0 ? lyricsData.Lyrics.Select(l => l.TimePoint).OrderBy(t => t).ToList() : [];

    [ObservableProperty]
    public partial int LyricsIndex { get; set; }

    [ObservableProperty]
    public partial LyricLine CurrentLyric { get; private set; } = lyricsData.Lyrics.FirstOrDefault();

    public LyricsData LyricsData { get; } = lyricsData;

    public void UpdateLyricsIndex(double timePoints)
    {
        LyricsIndex = CalculateIndex(timePoints);

        // 确保索引有效
        if (LyricsIndex < 0 || LyricsIndex >= LyricsData.Lyrics.Count)
            return;

        CurrentLyric = LyricsData.Lyrics[LyricsIndex];
        CurrentLyrics?.Invoke(this, LyricsIndex, CurrentLyric);
    }

    /// <summary>
    /// 根据当前播放时间计算当前歌词索引
    /// </summary>
    /// <param name="currentTime">当前播放时间（秒）</param>
    /// <returns>当前歌词索引，如果没有匹配的歌词则返回-1</returns>
    public int CalculateIndex(double currentTime)
    {
        if (_timePoints.Count == 0)
            return -1;

        // 如果当前时间小于第一个时间点，返回-1
        if (currentTime < _timePoints[0])
            return -1;

        // 如果当前时间大于等于最后一个时间点，返回最后一个索引
        if (currentTime >= _timePoints[^1])
            return _timePoints.Count - 1;

        // 二分查找
        int left = 0;
        int right = _timePoints.Count - 1;

        while (left <= right)
        {
            int mid = left + (right - left) / 2;

            if (mid == _timePoints.Count - 1 || currentTime >= _timePoints[mid] && currentTime < _timePoints[mid + 1])
                return mid;

            if (currentTime < _timePoints[mid])
                right = mid - 1;
            else
                left = mid + 1;
        }

        // 如果没有找到匹配的时间点，返回最接近的前一个时间点的索引
        for (int i = _timePoints.Count - 1; i >= 0; i--)
        {
            if (currentTime >= _timePoints[i])
                return i;
        }

        return -1;
    }

    /// <summary>
    /// 获取指定索引对应的时间点
    /// </summary>
    /// <param name="index">歌词索引</param>
    /// <returns>时间点（秒），如果索引无效则返回0</returns>
    public double GetTimePointByIndex(int index)
    {
        if (index < 0 || index >= _timePoints.Count)
            return 0;

        return _timePoints[index];
    }

    /// <summary>
    /// 获取时间点列表
    /// </summary>
    /// <returns>时间点列表</returns>
    public IReadOnlyList<double> GetTimePoints() => _timePoints;

    /// <summary>
    /// 获取歌词总数
    /// </summary>
    public int Count => _timePoints.Count;
}

public class LyricsData
{
    // 歌词元数据
    public string? Title { get; set; }
    public string? Artist { get; set; }
    public string? Album { get; set; }
    public string? Creator { get; set; }
    public double Offset { get; set; }

    public List<LyricLine> Lyrics { get; set; } = [new(0, "暂无歌词")];

    /// 判断是否有翻译
    public bool HasTranslation => Lyrics.Any(line => !string.IsNullOrEmpty(line.Translation));
}
