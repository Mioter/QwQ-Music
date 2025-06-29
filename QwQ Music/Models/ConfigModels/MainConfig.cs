using System.IO;
using QwQ_Music.Utilities;

namespace QwQ_Music.Models.ConfigModels;

public static class MainConfig
{
    public static string ConfigSavePath =>
        PathEnsurer.EnsureDirectoryExists(Path.Combine(Directory.GetCurrentDirectory(), "config"));

    public static string LogSavePath =>
        PathEnsurer.EnsureDirectoryExists(Path.Combine(Directory.GetCurrentDirectory(), "logs"));

    public static string DatabaseSavePath =>
        PathEnsurer.EnsureFileAndDirectoryExist(Path.Combine(Directory.GetCurrentDirectory(), "data", "music.QwQ.db"));

    public static string MusicCoverSavePath =>
        PathEnsurer.EnsureDirectoryExists(Path.Combine(Directory.GetCurrentDirectory(), "cache", "music-cover"));

    public static string PlaylistCoverSavePath =>
        PathEnsurer.EnsureDirectoryExists(Path.Combine(Directory.GetCurrentDirectory(), "cache", "playlist-cover"));

    public static string LyricsSavePath =>
        PathEnsurer.EnsureDirectoryExists(Path.Combine(Directory.GetCurrentDirectory(), "cache", "lyrics"));
}
