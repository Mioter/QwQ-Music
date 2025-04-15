using System;
using System.Collections.Generic;
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
                var primaryLyrics = new Dictionary<double, string>();
                var translationLyrics = new Dictionary<double, string>();
                bool isTranslation = false;
                bool hasFoundTranslation = false;

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

                        switch (key)
                        {
                            case "ti": lyricsData.Title = value; break;
                            case "ar": lyricsData.Artist = value; break;
                            case "al": lyricsData.Album = value; break;
                            case "by": lyricsData.Creator = value; break;
                            case "offset": 
                                if (double.TryParse(value, out double offset))
                                    lyricsData.Offset = offset;
                                break;
                        }
                        continue;
                    }

                    // 检测是否为翻译歌词部分的开始
                    // 如果发现重复的元数据，可能是翻译部分的开始
                    if (trimmedLine.StartsWith("[ti:") && hasFoundTranslation == false)
                    {
                        isTranslation = true;
                        hasFoundTranslation = true;
                        continue;
                    }

                    // 处理时间戳和歌词
                    var matches = TimeRegex().Matches(trimmedLine);
                    if (matches.Count <= 0) continue;

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

                        // 根据是否为翻译部分，将歌词存入相应的字典
                        if (isTranslation)
                            translationLyrics[timeInSeconds] = lyric;
                        else
                            primaryLyrics[timeInSeconds] = lyric;
                    }
                }

                lyricsData.PrimaryLyrics = primaryLyrics;
                lyricsData.TranslationLyrics = translationLyrics.Count > 0 ? translationLyrics : null;

                return lyricsData;
            });
        }
        catch (Exception ex)
        {
            Log.Error($"解析歌词文件出错：{ex.Message}");
            return null;
        }
    }
}
