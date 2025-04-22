using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using QwQ_Music.Models;
using Log = QwQ_Music.Services.LoggerService;

namespace QwQ_Music.Services;

/// <summary>
/// 提供歌词解析和处理功能的服务
/// </summary>
public static partial class LyricsService
{
    // 歌词元数据正则表达式 - 匹配如 [ti:标题] [ar:艺术家] 等格式
    [GeneratedRegex(@"\[(ti|ar|al|by|offset):([^\]]*)\]")]
    private static partial Regex MetadataRegex();

    // 时间戳正则表达式 - 支持 [mm:ss.xx] 和 [mm:ss.xxx] 格式
    [GeneratedRegex(@"\[(\d{2}):(\d{2})\.(\d{2,3})\]")]
    private static partial Regex TimeRegex();

    /// <summary>
    /// 解析LRC格式的歌词文本
    /// </summary>
    /// <param name="lyrics">LRC格式的歌词文本</param>
    /// <returns>解析后的歌词数据对象，如果解析失败则返回null</returns>
    public static LyricsData? ParseLrcFile(string lyrics)
    {
        try
        {
            if (string.IsNullOrEmpty(lyrics))
                return null;

            var lyricsData = new LyricsData();
            string[] lines = lyrics.Split('\n');

            // 第一步：检测是否有双语歌词
            var duplicateTimeStamps = DetectDualLyrics(lines);

            // 第二步：解析歌词内容
            var (primaryLyrics, translationLyrics) = ParseLyricsContent(lines, duplicateTimeStamps, lyricsData);

            // 第三步：清理和合并歌词
            var mergedLyrics = MergeLyrics(primaryLyrics, translationLyrics);

            // 设置歌词数据并按时间点排序
            if (mergedLyrics.Count > 0)
                lyricsData.Lyrics = mergedLyrics.OrderBy(l => l.TimePoint).ToList();

            return lyricsData;

        }
        catch (Exception ex)
        {
            Log.Error($"解析歌词文件出错：{ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// 检测歌词文件是否包含双语歌词
    /// </summary>
    /// <param name="lines">歌词文件的所有行</param>
    /// <returns>重复的时间戳集合，用于后续判断双语歌词</returns>
    private static HashSet<string> DetectDualLyrics(string[] lines)
    {
        var timeStamps = new HashSet<string>();
        var duplicateTimeStamps = new HashSet<string>();
        
        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmedLine)) continue;
            
            var matches = TimeRegex().Matches(trimmedLine);
            if (matches.Count <= 0) continue;
            
            foreach (Match match in matches)
            {
                string timeStamp = match.Value;
                if (!timeStamps.Add(timeStamp))
                {
                    duplicateTimeStamps.Add(timeStamp);
                }
            }
        }
        
        return duplicateTimeStamps;
    }
    
    /// <summary>
    /// 解析歌词内容，包括元数据和时间戳歌词
    /// </summary>
    /// <param name="lines">歌词文件的所有行</param>
    /// <param name="duplicateTimeStamps">重复的时间戳集合</param>
    /// <param name="lyricsData">歌词数据对象，用于存储元数据</param>
    /// <returns>包含主歌词和翻译歌词的元组</returns>
    private static (Dictionary<double, string> primaryLyrics, Dictionary<double, string> translationLyrics) 
        ParseLyricsContent(string[] lines, HashSet<string> duplicateTimeStamps, LyricsData lyricsData)
    {
        var primaryLyrics = new Dictionary<double, string>();
        var translationLyrics = new Dictionary<double, string>();
        bool isTranslationPart = false;
        var timeStamps = new HashSet<string>();
        
        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmedLine)) continue;

            // 处理元数据
            var metadataMatch = MetadataRegex().Match(trimmedLine);
            if (metadataMatch.Success)
            {
                ProcessMetadata(metadataMatch, lyricsData, ref isTranslationPart, primaryLyrics);
                continue;
            }

            // 处理时间戳和歌词
            var matches = TimeRegex().Matches(trimmedLine);
            if (matches.Count <= 0) continue;

            // 提取歌词部分（去掉时间戳）
            string lyricText = TimeRegex().Replace(trimmedLine, "").Trim();
            
            // 检查是否进入翻译部分（通过检测重复的时间戳）
            if (!isTranslationPart && duplicateTimeStamps.Count > 0)
            {
                foreach (Match match in matches)
                {
                    string timeStamp = match.Value;
                    if (duplicateTimeStamps.Contains(timeStamp) && !timeStamps.Add(timeStamp))
                    {
                        isTranslationPart = true;
                        break;
                    }
                }
            }

            // 处理每个时间戳
            foreach (Match match in matches)
            {
                double timeInSeconds = ConvertTimeStampToSeconds(match);
                
                // 根据是否为翻译部分，将歌词存入相应的字典
                if (isTranslationPart)
                    translationLyrics[timeInSeconds] = lyricText;
                else
                    primaryLyrics[timeInSeconds] = lyricText;
            }
        }
        
        return (primaryLyrics, translationLyrics);
    }
    
    /// <summary>
    /// 处理歌词元数据
    /// </summary>
    /// <param name="metadataMatch">元数据正则匹配结果</param>
    /// <param name="lyricsData">歌词数据对象</param>
    /// <param name="isTranslationPart">是否为翻译部分的引用</param>
    /// <param name="primaryLyrics">主歌词字典</param>
    private static void ProcessMetadata(Match metadataMatch, LyricsData lyricsData, 
        ref bool isTranslationPart, Dictionary<double, string> primaryLyrics)
    {
        string key = metadataMatch.Groups[1].Value.ToLower();
        string value = metadataMatch.Groups[2].Value;

        // 检测是否为翻译歌词部分的开始
        // 如果发现重复的元数据标记(ti)，且已有主歌词，则标记为翻译部分
        if (key == "ti" && !isTranslationPart && primaryLyrics.Count > 0)
        {
            isTranslationPart = true;
            return;
        }

        if (isTranslationPart)
            return;
        
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
    
    /// <summary>
    /// 将时间戳转换为秒数
    /// </summary>
    /// <param name="match">时间戳正则匹配结果</param>
    /// <returns>转换后的秒数</returns>
    private static double ConvertTimeStampToSeconds(Match match)
    {
        int minutes = int.Parse(match.Groups[1].Value);
        int seconds = int.Parse(match.Groups[2].Value);

        // 处理毫秒，支持两位或三位
        string msStr = match.Groups[3].Value;
        double milliseconds = int.Parse(msStr);

        switch (msStr.Length)
        {
            case 2:
                milliseconds /= 100.0; // 两位数，如 [00:00.00]
                break;
            case 3:
                milliseconds /= 1000.0; // 三位数，如 [00:00.000]
                break;
        }

        return minutes * 60 + seconds + milliseconds;
    }
    
    /// <summary>
    /// 合并主歌词和翻译歌词
    /// </summary>
    /// <param name="primaryLyrics">主歌词字典</param>
    /// <param name="translationLyrics">翻译歌词字典</param>
    /// <returns>合并后的歌词行列表</returns>
    private static List<LyricLine> MergeLyrics(Dictionary<double, string> primaryLyrics, 
        Dictionary<double, string> translationLyrics)
    {
        var mergedLyrics = new List<LyricLine>();
        
        // 清理歌词，处理连续的空白歌词时间点
        var cleanedPrimaryLyrics = CleanLyrics(primaryLyrics);
        var cleanedTranslationLyrics = CleanLyrics(translationLyrics);

        switch (cleanedPrimaryLyrics.Count)
        {
            // 合并歌词 - 有主歌词也有翻译歌词
            case > 0 when cleanedTranslationLyrics.Count > 0:
            {
                // 合并两部分歌词的所有时间点
                var allTimePoints = new HashSet<double>(cleanedPrimaryLyrics.Keys);
                allTimePoints.UnionWith(cleanedTranslationLyrics.Keys);

                foreach (double timePoint in allTimePoints)
                {
                    cleanedPrimaryLyrics.TryGetValue(timePoint, out string? primary);
                    cleanedTranslationLyrics.TryGetValue(timePoint, out string? translation);

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
            // 只有主歌词
            case > 0:
                mergedLyrics.AddRange(cleanedPrimaryLyrics.Select(pair => 
                    new LyricLine(pair.Key, pair.Value)));
                break;
            // 只有翻译歌词，作为主歌词使用
            default:
                if (cleanedTranslationLyrics.Count > 0)
                {
                    mergedLyrics.AddRange(cleanedTranslationLyrics.Select(pair => 
                        new LyricLine(pair.Key, pair.Value)));
                }
                break;
        }
        
        return mergedLyrics;
    }

    /// <summary>
    /// 清理歌词，处理连续的空白歌词时间点
    /// </summary>
    /// <param name="lyrics">原始歌词字典</param>
    /// <returns>清理后的歌词字典</returns>
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
                while (j < sortedTimes.Count && string.IsNullOrWhiteSpace(lyrics[sortedTimes[j]]))
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
