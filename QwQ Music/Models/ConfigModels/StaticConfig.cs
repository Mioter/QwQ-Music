using QwQ_Music.Common.Utilities;

namespace QwQ_Music.Models.ConfigModels;

public static class StaticConfig {
    public static string ConfigSavePath =>
        PathEnsurer.EnsureDirectoryExists(Path.Combine(Directory.GetCurrentDirectory(), "config"));

    public static string LogSavePath =>
        PathEnsurer.EnsureDirectoryExists(Path.Combine(Directory.GetCurrentDirectory(), "logs"));

    public static string DatabasePath =>
        PathEnsurer.EnsureFileAndDirectoryExist(Path.Combine(Directory.GetCurrentDirectory(), "data", "music.QwQ.db"));

    public static string CachePath =>
        PathEnsurer.EnsureFileAndDirectoryExist(Path.Combine(Directory.GetCurrentDirectory(), "cache", "cache.QwQ.db"));

    public static string PlaylistPath =>
        PathEnsurer.EnsureFileAndDirectoryExist(
            Path.Combine(Directory.GetCurrentDirectory(), "data", "playlist.QwQ.playlist"));

    public static string LyricsSavePath =>
        PathEnsurer.EnsureDirectoryExists(Path.Combine(Directory.GetCurrentDirectory(), "cache", "lyrics"));
    
    public static string I18NSavePath =>
        PathEnsurer.EnsureDirectoryExists(Path.Combine(Directory.GetCurrentDirectory(), "i18n"));

}