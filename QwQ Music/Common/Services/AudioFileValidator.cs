using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace QwQ_Music.Common.Services;

public static class AudioFileValidator
{
    public enum ExtendAudioFormats
    {
        Ncm,
    }

    public static readonly List<string> SupportedAudioFormats =
    [
        ".AAC",
        ".MP4",
        ".M4A",
        ".M4B",

        // Apple Core Audio
        ".CAF",

        // Audible
        ".AAX",
        ".AA",

        // Audio Interchange File Format
        ".AIF",
        ".AIFF",
        ".AIFC",

        // Digital Theatre System
        ".DTS",

        // Direct Stream Digital
        ".DSD",
        ".DSF",

        // Dolby Digital
        ".AC3",

        // Extended Module
        ".XM",

        // Free Lossless Audio Codec
        ".FLAC",

        // Genesis YM2612
        ".GYM",

        // Impulse Tracker
        ".IT",

        // Matroska Audio, WebM Audio
        ".MKA",
        ".WEBM",

        // Musical Instruments Digital Interface
        ".MID",
        ".MIDI",

        // Monkey's Audio
        ".APE",

        // MPEG Audio Layer
        ".MP1",
        ".MP2",
        ".MP3",

        // MusePack / MPEGplus
        ".MPC",
        ".MP+",

        // Noisetracker/Soundtracker/Protracker
        ".MOD",

        // OGG : Vorbis, Opus, Embedded FLAC, Speex
        ".OGG",
        ".OGA",
        ".OPUS",
        ".SPX",

        // OptimFROG
        ".OFR",
        ".OFS",

        // Portable Sound Format
        ".PSF",
        ".PSF1",
        ".PSF2",
        ".MINIPSF",
        ".MINIPSF1",
        ".MINIPSF2",
        ".SSF",
        ".MINISSF",
        ".GSF",
        ".MINIGSF",
        ".QSF",
        ".MINIQSF",

        // ScreamTracker
        ".S3M",

        // SPC700 (Super Nintendo Sound files)
        ".SPC",

        // Toms' losslesss Audio Kompressor
        ".TAK",

        // True Audio
        ".TTA",

        // TwinVQ
        ".VQF",

        // PCM (uncompressed audio)
        ".WAV",
        ".BWAV",
        ".BWF",

        // Video Game Music (SEGA systems sound files)
        ".VGM",
        ".VGZ",

        // WavPack
        ".WV",

        // Windows Media Audio/Advanced Systems Format
        ".WMA",
        ".ASF",
    ];

    public static Dictionary<ExtendAudioFormats, string> AudioFormatsExtendToNameMap { get; } =
        new()
        {
            [ExtendAudioFormats.Ncm] = ".NCM",
        };

    private static bool IsAudioFile(string filePath)
    {
        return File.Exists(filePath) && ValidateWithMetadata(filePath);
    }

    private static bool ValidateWithMetadata(string path)
    {
        try
        {
            return SupportedAudioFormats.Contains($"{Path.GetExtension(path).ToUpper()}")
             || AudioFormatsExtendToNameMap.ContainsValue(Path.GetExtension(path).ToUpper());
        }
        catch
        {
            return false;
        }
    }

    public static List<string>? FilterAudioFiles(IReadOnlyList<string>? items)
    {
        return items?.Where(IsAudioFile).ToList();
    }
}
