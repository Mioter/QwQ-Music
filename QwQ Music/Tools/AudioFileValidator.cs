using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NAudio.Wave;
using TagLib;
using File = System.IO.File;

namespace QwQ_Music.Tools;

public static class AudioFileValidator
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3",
        ".wav",
        ".flac",
        ".aac",
        ".ogg",
        ".m4a",
        ".wma",
        ".aiff",
        ".alac",
        ".opus",
    };

    // ReSharper disable once MemberCanBePrivate.Global
    public static bool IsAudioFile(string filePath)
    {
        if (!File.Exists(filePath) || !SupportedExtensions.Contains(Path.GetExtension(filePath)))
            return false;

        return ValidateWithTagLib(filePath) && ValidateWithNAudio(filePath);
    }

    private static bool ValidateWithTagLib(string path)
    {
        try
        {
            using var file = TagLib.File.Create(path);
            return file.Properties.MediaTypes != MediaTypes.None;
        }
        catch
        {
            return false;
        }
    }

    private static bool ValidateWithNAudio(string path)
    {
        try
        {
            using var reader = new AudioFileReader(path);
            return reader.WaveFormat.SampleRate > 0 && reader.TotalTime.TotalSeconds > 0;
        }
        catch
        {
            return false;
        }
    }

    public static List<string>? FilterAudioFiles(List<string>? items)
    {
        return items?
            .Where(IsAudioFile)
            .ToList();
    }
}
