using System.Collections.Generic;
using System.Linq;

namespace QwQ_Music.Utilities;

/// <summary>
/// 歌词索引计算器，用于根据当前播放时间计算当前歌词索引
/// </summary>
public class LyricIndexCalculator(LyricsData lyricsData)
{
    private readonly List<double> _timePoints =
        lyricsData.PrimaryLyrics.Count > 0 ? lyricsData.PrimaryLyrics.Keys.OrderBy(k => k).ToList() : [];

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
    /// 获取指定索引对应的歌词文本
    /// </summary>
    /// <param name="index">歌词索引</param>
    /// <returns>歌词文本，如果索引无效则返回空字符串</returns>
    public string GetLyricTextByIndex(int index)
    {
        if (index < 0 || index >= _timePoints.Count)
            return string.Empty;

        double timePoint = _timePoints[index];
        return lyricsData.PrimaryLyrics.TryGetValue(timePoint, out string? text) ? text : string.Empty;
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
