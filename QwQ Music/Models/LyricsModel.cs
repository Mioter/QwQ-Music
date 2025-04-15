using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using QwQ_Music.Utilities;

namespace QwQ_Music.Models;

public partial class LyricsModel(LyricsData lyricsData) : ObservableObject
{
    public delegate void CurrentLyricsChangedEventHandler(object sender, int lyricsIndex, string lyricsText);

    public event CurrentLyricsChangedEventHandler? CurrentLyrics;

    public LyricIndexCalculator LyricIndexCalculator { get; } = new(lyricsData);

    [ObservableProperty]
    public partial int LyricsIndex { get; private set; }

    [ObservableProperty]
    public partial string CurrentLyric { get; private set; } = lyricsData.PrimaryLyrics.FirstOrDefault().Value;

    public LyricsData LyricsData { get; } = lyricsData;

    public void UpdateLyricsIndex(double timePoints)
    {
        LyricsIndex = LyricIndexCalculator.CalculateIndex(timePoints);
        CurrentLyric = LyricIndexCalculator.GetLyricTextByIndex(LyricsIndex);
        CurrentLyrics?.Invoke(this, LyricsIndex, CurrentLyric);
    }
}
