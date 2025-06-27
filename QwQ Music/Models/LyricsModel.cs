using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace QwQ_Music.Models;

/// 歌词行结构体
public record struct LyricLine(double TimePoint, string Primary, string? Translation = null);

public partial class LyricsModel : ObservableObject
{
    public static int LyricOffset { get; set; }

    public delegate void LyricLineChangedEventHandler(object sender, LyricLine currentLyric, LyricLine? nextLyric);

    public event LyricLineChangedEventHandler? LyricLineChanged;

    private readonly List<double> _timePoints = [];

    [ObservableProperty]
    public partial int LyricsIndex { get; set; }

    [ObservableProperty]
    public partial LyricLine CurrentLyric { get; private set; }

    [ObservableProperty]
    public partial LyricLine NextLyricLine { get; private set; }

    [ObservableProperty]
    public partial LyricsData LyricsData { get; private set; } = new();

    /// <summary>
    /// 更新歌词数据
    /// </summary>
    /// <param name="lyricsData">新的歌词数据</param>
    public void UpdateLyricsData(LyricsData lyricsData)
    {
        LyricsData = lyricsData;
        _timePoints.Clear();

        if (lyricsData.Lyrics.Count > 0)
        {
            _timePoints.AddRange(lyricsData.Lyrics.Select(l => l.TimePoint).OrderBy(t => t));
            CurrentLyric = lyricsData.Lyrics[0];
            LyricsIndex = 0;
            NextLyricLine = lyricsData.Lyrics.Count > 1 ? lyricsData.Lyrics[1] : new LyricLine(0, null!);
        }
        else
        {
            CurrentLyric = new LyricLine(0, "暂无歌词");
            LyricsIndex = -1;
            NextLyricLine = new LyricLine(0, null!);
        }
    }

    /// <summary>
    /// 应用偏移量到时间点
    /// </summary>
    /// <param name="timePoint">原始时间点（秒）</param>
    /// <returns>应用偏移后的时间点（秒）</returns>
    private static double ApplyOffset(double timePoint)
    {
        // 将毫秒转换为秒并应用偏移
        return timePoint + LyricOffset / 1000.0;
    }

    /// <summary>
    /// 移除偏移量从时间点
    /// </summary>
    /// <param name="timePoint">带偏移的时间点（秒）</param>
    /// <returns>移除偏移后的时间点（秒）</returns>
    private static double RemoveOffset(double timePoint)
    {
        // 将毫秒转换为秒并移除偏移
        return timePoint - LyricOffset / 1000.0;
    }

    /// <summary>
    /// 获取当前歌词到下一句歌词的时间间隔
    /// </summary>
    /// <param name="currentTime">当前播放时间（秒）</param>
    /// <returns>到下一句歌词的时间间隔（秒），如果没有下一句则返回-1</returns>
    public double GetNextLyricsInterval(double currentTime)
    {
        // 应用偏移量到当前时间
        double adjustedTime = ApplyOffset(currentTime);
        int currentIndex = CalculateIndex(adjustedTime);
        if (currentIndex < 0 || currentIndex >= _timePoints.Count - 1)
            return -1;

        // 计算到下一句歌词的时间间隔（考虑偏移量）
        return _timePoints[currentIndex + 1] - adjustedTime;
    }

    /// <summary>
    /// 获取下一句歌词
    /// </summary>
    /// <param name="currentTime">当前播放时间（秒）</param>
    /// <returns>下一句歌词，如果没有下一句则返回null</returns>
    public LyricLine? GetNextLyric(double currentTime)
    {
        // 应用偏移量到当前时间
        double adjustedTime = ApplyOffset(currentTime);
        int currentIndex = CalculateIndex(adjustedTime);
        if (currentIndex < 0 || currentIndex >= _timePoints.Count - 1)
            return null;

        return LyricsData.Lyrics[currentIndex + 1];
    }

    public void UpdateLyricsIndex(double timePoints)
    {
        // 应用偏移量到当前时间
        double adjustedTime = ApplyOffset(timePoints);
        int newIndex = CalculateIndex(adjustedTime);

        // 确保索引有效
        if (newIndex < 0 || newIndex >= LyricsData.Lyrics.Count)
            return;

        LyricsIndex = newIndex;
        CurrentLyric = LyricsData.Lyrics[LyricsIndex];
        NextLyricLine =
            LyricsIndex + 1 < LyricsData.Lyrics.Count ? LyricsData.Lyrics[LyricsIndex + 1] : new LyricLine(0, null!);

        // 触发歌词变更事件，同时传递当前歌词和下一句歌词
        LyricLineChanged?.Invoke(this, CurrentLyric, GetNextLyric(timePoints));
    }

    /// <summary>
    /// 根据当前播放时间计算当前歌词索引
    /// </summary>
    /// <param name="currentTime">当前播放时间（秒）</param>
    /// <returns>当前歌词索引，如果没有匹配的歌词则返回-1</returns>
    public int CalculateIndex(double currentTime)
    {
        switch (_timePoints.Count)
        {
            case 0:
                return -1;
            // 如果只有一行歌词，则始终显示它
            case 1:
                return 0;
        }

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
