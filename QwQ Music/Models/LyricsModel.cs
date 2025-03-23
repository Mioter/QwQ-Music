using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Log = QwQ_Music.Services.LoggerService;

namespace QwQ_Music.Models;

public record struct LyricsModel(IReadOnlyList<uint> Indexes, IReadOnlyList<IReadOnlyList<string>> Lyrics)
    : IEnumerable<KeyValuePair<uint, IReadOnlyList<string>>>
{
    private int _prev = 0;

    public IReadOnlyList<string> GetCurrentLyrics(uint t)
    {
        while (t < Indexes[_prev])
            _prev--;
        while (t > Indexes[_prev])
            _prev++;
        return Lyrics[_prev];
    }

    public static readonly LyricsModel Empty = new([], []);

    public async static Task<LyricsModel> ParseAsync(string lyrics)
    {
        return await Task.Run(() =>
            {
                if (string.IsNullOrEmpty(lyrics))
                    return Empty;
                List<uint> indexes = [];
                List<List<string>> result = [];
                string[] data = lyrics.Split("\n");
                try
                {
                    foreach (string? line in data)
                    {
                        if (!TimeSpan.TryParse(line[1..9], out var time))
                            continue;
                        uint t = (uint)(time.TotalSeconds * 100);

                        if (indexes[^1] == t)
                            result[^1].Add(line[9..].Trim());
                        else
                        {
                            indexes.Add(t);
                            result.Add([line[9..].Trim()]);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning("Cannot parse lyrics:" + ex.Message);
                    indexes = [0];
                    result =
                    [
                        [lyrics],
                    ];
                }

                return new LyricsModel(indexes, result);
            })
            .ConfigureAwait(false);
    }

    public void Reset() => _prev = 0;

    public IEnumerator<KeyValuePair<uint, IReadOnlyList<string>>> GetEnumerator()
    {
        foreach ((uint index, var lyric) in Indexes.Zip(Lyrics, Tuple.Create))
        {
            yield return new KeyValuePair<uint, IReadOnlyList<string>>(index, lyric);
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
