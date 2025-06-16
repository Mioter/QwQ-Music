using System.IO;
using QwQ_Music.Utilities;

namespace QwQ_Music.Models.ConfigModel;

public class MainConfig
{
    public static string DatabaseSavePath => Path.Combine(Directory.GetCurrentDirectory(), "config", "data.db");

    public static string MusicCoverSavePath =>
        PathEnsurer.EnsureDirectoryExists(Path.Combine(Directory.GetCurrentDirectory(), "cache", "music-cover"));

    public static string PlaylistCoverSavePath =>
        PathEnsurer.EnsureDirectoryExists(Path.Combine(Directory.GetCurrentDirectory(), "cache", "playlist-cover"));

    public static string LyricsSavePath =>
        PathEnsurer.EnsureDirectoryExists(Path.Combine(Directory.GetCurrentDirectory(), "cache", "lyrics"));
}
