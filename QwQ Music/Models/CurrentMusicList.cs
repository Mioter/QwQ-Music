using Avalonia.Collections;

namespace QwQ_Music.Models;

public class CurrentMusicList
{
    public string IdStr { get; set; } = string.Empty;

    public AvaloniaList<MusicItemModel> MusicItems { get; set; } = [];
}
