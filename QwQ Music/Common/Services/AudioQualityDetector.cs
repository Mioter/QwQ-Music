using ATL;
using QwQ_Music.Models.Enums;

namespace QwQ_Music.Common.Services;

/// <summary>
///     音频质量检测器，用于根据音频文件的技术参数判断音质级别
/// </summary>
public static class AudioQualityDetector {
    public static readonly string[]
        LosslessFormats = ["FLAC", "APE", "WAV", "TTA", "WV", "TAK", "ALAC", "WMA LOSSLESS"];

    /// <summary>
    ///     根据音频扩展信息确定音质级别
    /// </summary>
    /// <param name="track">音频扩展信息</param>
    /// <returns>音质级别</returns>
    public static AudioQualityLevel DetermineQualityLevel(Track track) {
        int sampleRate = (int)track.SampleRate;
        int bitrate = track.Bitrate;
        int bitDepth = track.BitDepth;
        string audioFormat = track.AudioFormat.ShortName.ToUpperInvariant();
        bool isVbr = track.IsVBR;

        // 首先检查是否为无损格式
        if (IsLosslessFormat(audioFormat))
            return DetermineLosslessQuality(sampleRate, bitDepth);

        // 检查是否为高解析度音频
        return IsHighResolution(sampleRate, bitDepth) ?
            AudioQualityLevel.HR :
            DetermineLossyQuality(bitrate, sampleRate, isVbr); // 对于有损格式，根据比特率判断
    }

    /// <summary>
    ///     判断是否为无损格式
    /// </summary>
    private static bool IsLosslessFormat(string audioFormat) { return LosslessFormats.Any(audioFormat.Contains); }

    /// <summary>
    ///     判断是否为高解析度音频
    /// </summary>
    private static bool IsHighResolution(int sampleRate, int bitsPerSample) {
        // 高解析度音频标准：采样率 > 48kHz 或 位深度 > 16bit
        return sampleRate > 48000 || bitsPerSample > 16;
    }

    /// <summary>
    ///     确定无损格式的音质级别
    /// </summary>
    private static AudioQualityLevel DetermineLosslessQuality(int sampleRate, int bitsPerSample) {
        return IsHighResolution(sampleRate, bitsPerSample) ? AudioQualityLevel.HR : AudioQualityLevel.SQ;
    }

    /// <summary>
    ///     确定有损格式的音质级别
    /// </summary>
    private static AudioQualityLevel DetermineLossyQuality(int bitrate, int sampleRate, bool isVbr) {
        // 对于VBR，使用平均比特率进行判断
        // 对于CBR，直接使用比特率
        return sampleRate switch {
            // MP3 音质判断标准
            >= 44100 when bitrate is >= 320 or >= 256 or >= 192 => AudioQualityLevel.HQ,

            >= 44100 when bitrate >= 128 => AudioQualityLevel.PQ,
            >= 44100                     => AudioQualityLevel.Poor,

            // 低采样率
            >= 22050 when bitrate >= 128 => AudioQualityLevel.PQ,
            _                            => AudioQualityLevel.Poor
        };
    }


    /// <summary>
    ///     获取音质级别的中文描述
    /// </summary>
    /// <param name="qualityLevel">音质级别</param>
    /// <returns>中文描述</returns>
    public static string GetQualityDescription(AudioQualityLevel qualityLevel) {
        return qualityLevel switch {
            AudioQualityLevel.Unknown => "未知",
            AudioQualityLevel.Poor    => "低质",
            AudioQualityLevel.PQ      => "普通",
            AudioQualityLevel.HQ      => "高品质",
            AudioQualityLevel.SQ      => "无损",
            AudioQualityLevel.HR      => "高解析",
            _                         => "未知"
        };
    }

    /// <summary>
    ///     获取音质级别的详细描述
    /// </summary>
    /// <param name="qualityLevel">音质级别</param>
    /// <returns>详细描述</returns>
    public static string GetQualityDetailedDescription(AudioQualityLevel qualityLevel) {
        return qualityLevel switch {
            AudioQualityLevel.Unknown => "无法识别的音质级别",
            AudioQualityLevel.Poor    => "低质量音频，通常比特率较低或采样率不足",
            AudioQualityLevel.PQ      => "标准质量音频，适合日常收听",
            AudioQualityLevel.HQ      => "高质量音频，提供更好的听觉体验",
            AudioQualityLevel.SQ      => "无损音频，保持原始音质",
            AudioQualityLevel.HR      => "高解析度音频，超越CD音质",
            _                         => "未知音质级别"
        };
    }
}