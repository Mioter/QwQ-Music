using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using QwQ_Music.Models;
using Log = QwQ_Music.Services.LoggerService;

namespace QwQ_Music.Services;

public partial record LyricsService
{
    // 歌词元数据正则表达式
    [GeneratedRegex(@"\[(ti|ar|al|by|offset):([^\]]*)\]")]
    private static partial Regex MetadataRegex();

    // 时间戳正则表达式 - 支持 [mm:ss.xx] 和 [mm:ss.xxx] 格式
    [GeneratedRegex(@"\[(\d{2}):(\d{2})\.(\d{2,3})\]")]
    private static partial Regex TimeRegex();

    public static async Task<LyricsData?> ParseLrcFile(string lyrics)
    {
        try
        {
            return await Task.Run(() =>
            {
                if (string.IsNullOrEmpty(lyrics))
                    return null;

                var lyricsData = new LyricsData();
                var mergedLyrics = new List<LyricLine>();

                // 修改：使用两个独立的字典来分别存储原始歌词和翻译歌词
                var firstPartLyrics = new Dictionary<double, string>();
                var secondPartLyrics = new Dictionary<double, string>();
                bool isSecondPart = false;

                // 读取文件的所有行
                string[] lines = lyrics.Split('\n');

                // 处理元数据和歌词
                foreach (string line in lines)
                {
                    string trimmedLine = line.Trim();

                    // 跳过空行
                    if (string.IsNullOrWhiteSpace(trimmedLine))
                        continue;

                    // 处理元数据
                    var metadataMatch = MetadataRegex().Match(trimmedLine);
                    if (metadataMatch.Success)
                    {
                        string key = metadataMatch.Groups[1].Value.ToLower();
                        string value = metadataMatch.Groups[2].Value;

                        // 检测是否为翻译歌词部分的开始
                        // 如果发现重复的元数据，标记为第二部分
                        if (key == "ti" && isSecondPart == false && firstPartLyrics.Count > 0)
                        {
                            isSecondPart = true;
                            continue;
                        }

                        if (!isSecondPart)
                        {
                            switch (key)
                            {
                                case "ti":
                                    lyricsData.Title = value;
                                    break;
                                case "ar":
                                    lyricsData.Artist = value;
                                    break;
                                case "al":
                                    lyricsData.Album = value;
                                    break;
                                case "by":
                                    lyricsData.Creator = value;
                                    break;
                                case "offset":
                                    if (double.TryParse(value, out double offset))
                                        lyricsData.Offset = offset;
                                    break;
                            }
                        }
                        continue;
                    }

                    // 处理时间戳和歌词
                    var matches = TimeRegex().Matches(trimmedLine);
                    if (matches.Count <= 0)
                        continue;

                    // 提取歌词部分（去掉时间戳）
                    string lyric = TimeRegex().Replace(trimmedLine, "").Trim();

                    foreach (Match match in matches)
                    {
                        // 解析时间戳并转换为秒数
                        int minutes = int.Parse(match.Groups[1].Value);
                        int seconds = int.Parse(match.Groups[2].Value);

                        // 处理毫秒，支持两位或三位
                        string msStr = match.Groups[3].Value;
                        double milliseconds = int.Parse(msStr);

                        switch (msStr.Length)
                        {
                            // 根据毫秒位数调整
                            case 2:
                                milliseconds /= 100.0; // 两位数，如 [00:00.00]
                                break;
                            case 3:
                                milliseconds /= 1000.0; // 三位数，如 [00:00.000]
                                break;
                        }

                        double timeInSeconds = minutes * 60 + seconds + milliseconds;

                        // 根据是否为第二部分，将歌词存入相应的字典
                        if (isSecondPart)
                            secondPartLyrics[timeInSeconds] = lyric;
                        else
                            firstPartLyrics[timeInSeconds] = lyric;
                    }
                }

                // 清理歌词
                var cleanedFirstPart = CleanLyrics(firstPartLyrics);
                var cleanedSecondPart = CleanLyrics(secondPartLyrics);

                switch (cleanedFirstPart.Count)
                {
                    // 合并歌词
                    case > 0 when cleanedSecondPart.Count > 0:
                    {
                        // 合并两部分歌词
                        var allTimePoints = new HashSet<double>(cleanedFirstPart.Keys);
                        allTimePoints.UnionWith(cleanedSecondPart.Keys);

                        foreach (double timePoint in allTimePoints)
                        {
                            cleanedFirstPart.TryGetValue(timePoint, out string? primary);
                            cleanedSecondPart.TryGetValue(timePoint, out string? translation);

                            // 如果主歌词为空但翻译存在，则交换
                            if (string.IsNullOrEmpty(primary) && !string.IsNullOrEmpty(translation))
                            {
                                primary = translation;
                                translation = null;
                            }

                            mergedLyrics.Add(new LyricLine(timePoint, primary ?? string.Empty, translation));
                        }
                        break;
                    }
                    case > 0:
                        // 只有主歌词
                        mergedLyrics.AddRange(cleanedFirstPart.Select(pair => new LyricLine(pair.Key, pair.Value)));
                        break;
                    default:
                    {
                        if (cleanedSecondPart.Count > 0)
                        {
                            // 只有第二部分，作为主歌词
                            mergedLyrics.AddRange(
                                cleanedSecondPart.Select(pair => new LyricLine(pair.Key, pair.Value))
                            );
                        }
                        break;
                    }
                }

                // 设置歌词数据并按时间点排序
                if (mergedLyrics.Count > 0)
                    lyricsData.Lyrics = mergedLyrics.OrderBy(l => l.TimePoint).ToList();

                return lyricsData;
            });
        }
        catch (Exception ex)
        {
            await Log.ErrorAsync($"解析歌词文件出错：{ex.Message}");
            return null;
        }
    }

    // 清理歌词，处理连续的空白歌词时间点
    private static Dictionary<double, string> CleanLyrics(Dictionary<double, string> lyrics)
    {
        if (lyrics.Count <= 0)
            return new Dictionary<double, string>();

        var cleanedLyrics = new Dictionary<double, string>();
        var sortedTimes = new List<double>(lyrics.Keys);
        sortedTimes.Sort();

        for (int i = 0; i < sortedTimes.Count; i++)
        {
            double currentTime = sortedTimes[i];
            string currentLyric = lyrics[currentTime];

            // 如果当前歌词为空且不是最后一个时间点
            if (string.IsNullOrWhiteSpace(currentLyric) && i < sortedTimes.Count - 1)
            {
                // 查找连续的空白歌词
                int j = i + 1;
                while (j < sortedTimes.Count && string.IsNullOrWhiteSpace(lyrics[sortedTimes[j]])) // 1秒内的连续空白
                {
                    j++;
                }

                // 如果找到了连续的空白歌词，跳过它们
                if (j > i + 1)
                {
                    cleanedLyrics[currentTime] = currentLyric;
                    i = j - 1; // 跳过连续的空白歌词
                    continue;
                }
            }

            cleanedLyrics[currentTime] = currentLyric;
        }

        return cleanedLyrics;
    }
}
