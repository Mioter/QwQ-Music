using System.Text.RegularExpressions;
using QwQ_Music.Common.Helpers;
using QwQ_Music.Models;
using Log = QwQ_Music.Common.Services.LoggerService;

namespace QwQ_Music.Common.Services;

/// <summary>
///     提供歌词解析和处理功能的服务
/// </summary>
public static partial class LyricsService {
    // 歌词元数据正则表达式 - 匹配如 [ti:标题] [ar:艺术家] 等格式
    [GeneratedRegex(@"\[(ti|ar|al|by|offset):([^\]]*)\]")]
    private static partial Regex MetadataRegex();

    // 时间戳正则表达式 - 支持 [mm:ss.xx] 和 [mm:ss.xxx] 格式
    [GeneratedRegex(@"\[(\d{2}):(\d{2})\.(\d{2,3})\]")]
    private static partial Regex TimeRegex();

    /// <summary>
    ///     解析LRC格式的歌词文本
    /// </summary>
    /// <param name="lyrics">LRC格式的歌词文本</param>
    /// <returns>解析后的歌词数据对象，如果解析失败则返回null</returns>
    public static (double Offset, IEnumerable<LyricLine> Lyrics) ParseLrcFile(string lyrics) {
        try {
            if (string.IsNullOrEmpty(lyrics))
                return (0, []);

            string[] lines = lyrics.Split('\n');
            // 第一步：解析元数据
            double offset = ParseMetadata(lines);

            // 第二步：解析歌词内容
            IEnumerable<LyricLine> data = ParseLyricsContent(lines);

            return (offset, data);
        } catch (Exception ex) {
            Log.Error($"解析歌词文件出错：{ex.Message}");

            return (0, []);
        }
    }

    // metaone01: Only parse offset. Others use MusicItemModel's data.
    private static double ParseMetadata(IEnumerable<string> lines) {
        foreach (string line in lines) {
            string trimmedLine = line.Trim();

            if (string.IsNullOrWhiteSpace(trimmedLine))
                continue;

            Match metadataMatch = MetadataRegex().Match(trimmedLine);

            if (!metadataMatch.Success)
                return 0;

            string key = metadataMatch.Groups[1].Value.ToLower();
            string value = metadataMatch.Groups[2].Value;

            if (key == "offset" && double.TryParse(value, out double offset))
                return offset;
        }

        return 0;
    }

    /// <summary>
    ///     解析歌词内容，包括元数据和时间戳歌词
    /// </summary>
    /// <param name="lines">歌词文件的所有行</param>
    /// <returns>包含主歌词和翻译歌词的元组</returns>
    private static IEnumerable<LyricLine> ParseLyricsContent(IEnumerable<string> lines) {
        foreach (string line in lines) {
            string trimmedLine = line.Trim();

            if (string.IsNullOrWhiteSpace(trimmedLine))
                continue;

            // 处理时间戳和歌词
            MatchCollection matches = TimeRegex().Matches(trimmedLine);

            if (matches.Count == 0)
                continue;
            LyricLine lyric = new();
            TimeSpan? prev = null;
            // 提取歌词部分（去掉时间戳）
            foreach (Match match in matches) {
                TimeSpan curr = ParseHelper.TimeSpanParser(match.Value);
                string text = TimeRegex().Replace(trimmedLine, "").Trim();
                if (curr == prev) {
                    lyric.Secondary = text;
                    yield return lyric;
                    lyric = new LyricLine();
                    prev = null;
                    continue;
                }

                if (!string.IsNullOrEmpty(lyric.Primary)) {
                    yield return lyric;
                    lyric = new LyricLine();
                }

                prev = curr;
                lyric.TimePoint = curr.TotalSeconds;
                lyric.Primary = text;
            }
        }
    }
}