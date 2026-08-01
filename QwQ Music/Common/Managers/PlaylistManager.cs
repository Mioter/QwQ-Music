using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Common.Services;
using QwQ_Music.Common.Services.Databases;
using QwQ_Music.Models;
using QwQ_Music.Models.Enums;

namespace QwQ_Music.Common.Managers;

public partial class PlaylistManager : ObservableObject {
    public const string Custom = nameof(Custom);

    private const string Unknown = nameof(Unknown);

    public readonly List<PlaylistItemModel> SequentialPlaylist = [];


    private PlaylistManager() {
        Task.Run(async Task? () => {
                var (indexes, paths, count, latest) = await PlaylistRepository
                                                            .ParseAsync(
                                                                AudioPlayManager.PlayerConfig.LastPlayedFilePath)
                                                            .ConfigureAwait(false);
                if (Program.OpenWithFiles is null)
                    await ReplaceAsync(
                            MusicItemsManager.All.Name,
                            paths.Select((item, index) => {
                                     if (!MusicItemsManager.All.MusicItems.TryGetValue(
                                             item,
                                             out MusicItemModel? model)) {
                                         count--;
                                         if (index < latest)
                                             latest--;
                                         else if (index == latest)
                                             latest = 0;
                                     }

                                     return model;
                                 })
                                 .Where(item => item is not null)
                                 .Cast<MusicItemModel>(),
                            count,
                            latest,
                            ConfigManager.SystemConfig.IsPlayOnStart,
                            ConfigManager.SystemConfig.IsPlayOnStart,
                            indexes)
                        .ConfigureAwait(false);
            })
            .ContinueWith(LoggerService.HandleException)
            .ConfigureAwait(false);
    }

    public static PlaylistManager Instance { get; } = new();

    public PlaylistItemModel CurrentItem { get; set; } = PlaylistItemModel.RefDefault;

    public string CurrentListName { get; private set; } = Unknown;

    public PlayMode PlayMode {
        get => ConfigManager.PlayerConfig.PlayMode;
        set {
            if (value != PlayMode.Random)
                return;
            ActualPlaylist.Clear();
            ActualPlaylist.AddRange(SequentialPlaylist.Shuffle());
        }
    }

    public int CurrentIndex => ActualPlaylist.IndexOf(CurrentItem);

    public AvaloniaList<PlaylistItemModel> ActualPlaylist { get; } = [];


    public int Count => SequentialPlaylist.Count;

    public PlaylistItemModel First() {
        if (ActualPlaylist.Count == 0) {
            OrderedDictionary<string, MusicItemModel>.ValueCollection items = MusicItemsManager.All.MusicItems.Values;
            if (items.Count == 0)
                return PlaylistItemModel.RefDefault;
            ReplaceAsync(MusicItemsManager.All.Name, items, 0, true)
                .ContinueWith(LoggerService.HandleException)
                .ConfigureAwait(false);
        }

        return ActualPlaylist.FirstOrDefault(PlaylistItemModel.RefDefault);
    }


    public IEnumerable<PlaylistItemModel> Insert(
        PlaylistItemModel anchor,
        params IEnumerable<MusicItemModel> musicItems) {
        CurrentListName = Custom;
        PlaylistItemModel[] items = musicItems.Select(item => new PlaylistItemModel(item)).ToArray();
        SequentialPlaylist.InsertRange(SequentialPlaylist.IndexOf(anchor) + 1, items);
        ActualPlaylist.InsertRange(ActualPlaylist.IndexOf(anchor) + 1, items);
        return items;
    }

    public IEnumerable<PlaylistItemModel> Add(params IEnumerable<MusicItemModel> musicItems) {
        return Insert(ActualPlaylist.Last(), musicItems);
    }

    public IEnumerable<PlaylistItemModel> InsertToNext(params IEnumerable<MusicItemModel> musicItems) {
        return Insert(CurrentItem, musicItems);
    }

    [RelayCommand]
    public void AddSelectedToNext(params IEnumerable<MusicItemModel> musicItems) { Insert(CurrentItem, musicItems); }

    [RelayCommand]
    public void Remove(params IEnumerable<PlaylistItemModel> musicItems) {
        CurrentListName = Custom;
        PlaylistItemModel[] items = [.. musicItems];
        PlaylistItemModel next = ActualPlaylist.Skip(CurrentIndex)
                                               .FirstOrDefault(
                                                   item => !items.Contains(item),
                                                   PlaylistItemModel.RefDefault);
        SequentialPlaylist.RemoveAll(item => items.Contains(item));
        ActualPlaylist.RemoveAll(items);
        if (items.Contains(CurrentItem)) {
            _ = AudioPlayManager.Instance.SetMusicAsync(next, true, false)
                                .ContinueWith(LoggerService.HandleException)
                                .ConfigureAwait(false);
        }
    }

    public void RemoveAllOf(params IEnumerable<MusicItemModel> musicItems) {
        CurrentListName = Custom;
        MusicItemModel[] items = musicItems.ToArray();
        PlaylistItemModel[] playlistItems = SequentialPlaylist.AsParallel()
                                                              .Where(item => items.Contains(item.Model))
                                                              .ToArray();

        SequentialPlaylist.RemoveAll(item => playlistItems.Contains(item));
        ActualPlaylist.RemoveAll(playlistItems);
        if (items.Contains(AudioPlayManager.Instance.CurrentMusicItem.Model))
            AudioPlayManager.Instance.NextMusic();
    }

    public void Clear(bool isUserRequested) {
        CurrentListName = Custom;
        SequentialPlaylist.Clear();
        ActualPlaylist.Clear();
        PlaylistItemModel.Reset();
        AudioPlayManager.Instance.Stop(isUserRequested);
    }

    public async Task ReplaceAsync(
        string name,
        IEnumerable<MusicItemModel> musicItems,
        int capacity,
        int target,
        bool isUserRequested,
        bool isPlayNow = false,
        IEnumerable<int>? memorizedRandomOrder = null) {
        if (name is not Custom and not Unknown && CurrentListName == name && ActualPlaylist.Count == capacity) {
            await AudioPlayManager.Instance.SetMusicAsync(SequentialPlaylist[target], isPlayNow, isUserRequested)
                                  .ConfigureAwait(false);
            return;
        }


        AudioPlayManager.Instance.Pause(false);
        Clear(isUserRequested);
        SequentialPlaylist.EnsureCapacity(capacity);
        ActualPlaylist.EnsureCapacity(capacity);
        CurrentListName = name;
        if (PlayMode is PlayMode.SingleLoop) {
            var item = new PlaylistItemModel(musicItems.ElementAt(target));
            SequentialPlaylist.Add(item);
            ActualPlaylist.Add(item);
            NotificationService.Info($"已切换到{item.Model.Title} - {item.Model.Artists}");
            await AudioPlayManager.Instance.SetMusicAsync(item, isPlayNow, isUserRequested).ConfigureAwait(false);
            return;
        }

        if (memorizedRandomOrder is not null && PlayMode is PlayMode.Random) {
            //预填充List以扩充List<>._size
            SequentialPlaylist.AddRange(Enumerable.Repeat(PlaylistItemModel.RefDefault, capacity));
            List<PlaylistItemModel> actualPlaylist = [.. Enumerable.Repeat(PlaylistItemModel.RefDefault, capacity)];
            try {
                musicItems.Zip(memorizedRandomOrder)
                          .AsParallel()
                          .AsOrdered()
                          .Select((item, index) => (Index: index,
                                                    Item: item.First == MusicItemModel.Default ?
                                                        PlaylistItemModel.RefDefault :
                                                        new PlaylistItemModel(item.First), RandomOrder: item.Second))
                          .ForAll(data => {
                              SequentialPlaylist[data.Index] = data.Item;
                              actualPlaylist[data.RandomOrder] = data.Item;
                          });
                SequentialPlaylist.RemoveAll(item => item == PlaylistItemModel.RefDefault);
                ActualPlaylist.AddRange(actualPlaylist.Where(item => item != PlaylistItemModel.RefDefault));
                await AudioPlayManager.Instance.SetMusicAsync(SequentialPlaylist[target], isPlayNow, isUserRequested)
                                      .ConfigureAwait(false);
            } catch (IndexOutOfRangeException ex) {
                await LoggerService.ErrorAsync("无法恢复播放列表", ex).ConfigureAwait(false);
            }

            return;
        }

        SequentialPlaylist.AddRange(musicItems.Select(item => new PlaylistItemModel(item)));
        if (PlayMode is PlayMode.Random) {
            ActualPlaylist.Add(SequentialPlaylist[target]);
            ActualPlaylist.AddRange(SequentialPlaylist.Where((_, index) => index != target).Shuffle());
        } else {
            ActualPlaylist.AddRange(SequentialPlaylist);
        }

        if (ActualPlaylist.Count == 0)
            return;
        NotificationService.Info(
            $"已切换到歌单{(name == MusicItemsManager.All.Name ? I18NService.Lang.Translation["All Musics"] : name)}");
        await AudioPlayManager.Instance.SetMusicAsync(SequentialPlaylist[target], isPlayNow, isUserRequested)
                              .ConfigureAwait(false);
    }


    public async Task ReplaceAsync(
        string name,
        IList<MusicItemModel> musicItems,
        int target,
        bool isUserRequested,
        bool isPlayNow = false,
        IEnumerable<int>? memorizedRandomOrder = null) {
        if (musicItems.ElementAtOrDefault(target) != SequentialPlaylist.ElementAtOrDefault(target).Model)
            CurrentListName = Unknown;
        await ReplaceAsync(name, musicItems, musicItems.Count, target, isUserRequested, isPlayNow, memorizedRandomOrder)
            .ConfigureAwait(false);
    }
}