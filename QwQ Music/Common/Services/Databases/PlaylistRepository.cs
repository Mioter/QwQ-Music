using QwQ_Music.Models.ConfigModels;

namespace QwQ_Music.Common.Services.Databases;

public static class PlaylistRepository {
    // ReSharper disable once InconsistentNaming
    private const string QWQ_PLAYLIST_MEMORIZED_RANDOM_INDEXES = nameof(QWQ_PLAYLIST_MEMORIZED_RANDOM_INDEXES);

    public static async
        Task<(IEnumerable<int>? MemorizedRandomIndexes, IEnumerable<string> Paths, int Count, int Target)> ParseAsync(
            string? latestPath) {
        try {
            string[] data = await File.ReadAllLinesAsync(StaticConfig.PlaylistPath).ConfigureAwait(false);
            if (data.Length == 0) {
                await LoggerService.WarningAsync("存储的播放列表为空").ConfigureAwait(false);
                return (null, [], 0, 0);
            }

            int index = 1;
            if (latestPath is not null && data.IndexOf(latestPath) is { } val and not -1)
                index = val;

            if (!data[0].StartsWith(QWQ_PLAYLIST_MEMORIZED_RANDOM_INDEXES))
                return (null, data, data.Length, index);
            IEnumerable<int> indexes = data[0][(QWQ_PLAYLIST_MEMORIZED_RANDOM_INDEXES.Length + 1)..^1]
                                       .Split(',')
                                       .Select(int.Parse);
            return (indexes, data.Skip(1), data.Length - 1, index - 1);
        } catch (FileNotFoundException) {
            await LoggerService.WarningAsync("播放列表文件不存在").ConfigureAwait(false);
            return (null, [], 0, 0);
        }
    }

    public static async Task WriteAsync(IEnumerable<string> paths, IEnumerable<int>? memorizedRandomOrders = null) {
        if (memorizedRandomOrders is null) {
            await File.WriteAllLinesAsync(StaticConfig.PlaylistPath, paths).ConfigureAwait(false);
            return;
        }

        await using FileStream fs = File.OpenWrite(StaticConfig.PlaylistPath);
        await using var sw = new StreamWriter(fs);
        await sw.WriteLineAsync($"QWQ_PLAYLIST_MEMORIZED_RANDOM_INDEXES[{string.Join(',', memorizedRandomOrders)}]")
                .ConfigureAwait(false);
        foreach (string path in paths)
            await sw.WriteLineAsync(path).ConfigureAwait(false);
    }
}