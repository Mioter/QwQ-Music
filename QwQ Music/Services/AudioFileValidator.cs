using System.Collections.Generic;
using System.Linq;
using ATL;

namespace QwQ_Music.Services;

public static class AudioFileValidator
{

    private static bool IsAudioFile(string filePath)
    {
        return System.IO.File.Exists(filePath) && ValidateWithTagLib(filePath);

    }

    private static bool ValidateWithTagLib(string path)
    {
        try
        {
            var track = new Track(path);
            return track.AudioFormat != Format.UNKNOWN_FORMAT;
        }
        catch
        {
            return false;
        }
    }

    public static List<string>? FilterAudioFiles(List<string>? items)
    {
        return items?.Where(IsAudioFile).ToList();
    }
}
