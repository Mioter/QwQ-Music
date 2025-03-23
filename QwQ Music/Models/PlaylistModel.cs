using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using QwQ_Music.Services.ConfigIO;

namespace QwQ_Music.Models;

public class PlaylistModel(string name) : ObservableObject, IEnumerable<MusicItemModel>
{
    public readonly string Name = name;

    public ObservableCollection<MusicItemModel> MusicItems
    {
        get;
        set => SetProperty(ref field, value);
    } = [];

    public string LatestPlayedMusic = "";

    public int Count => MusicItems.Count;
    public bool IsInitialized { get; private set; }
    public bool IsError { get; private set; }

    public async Task LoadAsync()
    {
        var wait = DataBaseService
            .LoadFromDataBaseAsync(
                DataBaseService.Table.LISTNAMES,
                [nameof(LatestPlayedMusic)],
                reader => reader.GetFieldValue<string>(reader.GetOrdinal(nameof(LatestPlayedMusic))),
                ..1
            )
            .ConfigureAwait(false);
        await foreach (
            var item in DataBaseService.LoadFromDatabaseAsync<MusicItemModel>(
                DataBaseService.Table.PLAYLISTS,
                search: $"{nameof(Name)} = {Name}",
                table2: DataBaseService.Table.MUSICS
            )
        )
        {
            MusicItems.Add(item);
        }

        LatestPlayedMusic = (await wait)[0];
        IsError = false;
        IsInitialized = true;
    }

    public Dictionary<string, string> Dump() =>
        new()
        {
            [nameof(Name)] = Name,
            [nameof(Count)] = Count.ToString(),
            [nameof(LatestPlayedMusic)] = LatestPlayedMusic,
        };

    public IEnumerator<MusicItemModel> GetEnumerator() => MusicItems.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
