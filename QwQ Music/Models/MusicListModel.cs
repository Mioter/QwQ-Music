using System.Collections.Generic;

namespace QwQ_Music.Models;

public class MusicListModel(string? currentMusicPath, List<string?> musicPlayList)
{
    public string? CurrentMusicPath { get; set; } = currentMusicPath;

    public List<string?> MusicPlayList { get; set; } = musicPlayList;
}
