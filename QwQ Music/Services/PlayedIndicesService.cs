using System;
using System.Collections.Generic;
using System.Linq;

namespace QwQ_Music.Services;

public static class PlayedIndicesService
{
    // 添加一个列表来跟踪已播放的歌曲索引
    private static readonly List<int> _playedIndices = [];

    public static int GetNonRepeatingRandomIndex(int current, int count)
    {
        // 如果所有歌曲都已播放过（或者播放列表发生了变化），重置已播放列表
        if (_playedIndices.Count >= count || _playedIndices.Any(i => i >= count))
        {
            _playedIndices.Clear();
            _playedIndices.Add(current); // 将当前播放的歌曲添加到已播放列表
        }

        // 获取所有未播放的索引
        var availableIndices = Enumerable.Range(0, count).Where(i => !_playedIndices.Contains(i)).ToList();

        // 如果没有可用的索引（理论上不应该发生），返回一个随机索引
        if (availableIndices.Count == 0)
        {
            _playedIndices.Clear();
            _playedIndices.Add(current);
            availableIndices = Enumerable.Range(0, count).Where(i => !_playedIndices.Contains(i)).ToList();
        }

        // 从可用索引中随机选择一个
        var random = new Random();
        int randomIndex = availableIndices[random.Next(0, availableIndices.Count)];

        // 将选中的索引添加到已播放列表
        _playedIndices.Add(randomIndex);

        return randomIndex;
    }

    public static void ClearPlayedIndices()
    {
        _playedIndices.Clear();
    }
}
