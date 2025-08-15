using System;
using System.Threading.Tasks;
using QwQ_Music.Models;

namespace QwQ_Music.Common.Interfaces;

public interface IMusicPlayer
{
    /// <summary>
    ///     播放状态
    /// </summary>
    public bool IsPlaying { get; protected set; }

    /// <summary>
    ///     是否静音
    /// </summary>
    public bool IsMuted { get; set; }

    /// <summary>
    ///     音量（0-100）
    /// </summary>
    public int Volume { get; set; }

    /// <summary>
    ///     速率
    /// </summary>
    public float Speed { get; set; }

    /// <summary>
    ///     获取播放位置（秒）
    /// </summary>
    public double Position { get; set; }

    /// <summary>
    ///     更改播放状态
    /// </summary>
    public Task TogglePlayStace();

    /// <summary>
    ///     切换静音状态
    /// </summary>
    public void ToggleMute();

    /// <summary>
    ///     调整播放位置
    /// </summary>
    /// <param name="position">播放位置</param>
    public void Seek(double position);

    /// <summary>
    ///     下一首
    /// </summary>
    public Task NextSong();

    /// <summary>
    ///     上一首
    /// </summary>
    public Task PreviousSong();

    /// <summary>
    ///     播放此音乐
    /// </summary>
    /// <param name="musicItem">音乐项</param>
    public Task PlayThisMusic(MusicItemModel musicItem);

    /// <summary>
    ///     刷新播放状态
    /// </summary>
    public Task RefreshPlayback();

    /// <summary>
    ///     清除此音乐的播放时长
    /// </summary>
    /// <param name="musicItem">音乐项</param>
    public void ClearPlayDuration(MusicItemModel musicItem);

    /// <summary>
    ///     播放状态改变
    /// </summary>
    public event EventHandler<bool>? PlaybackStateChanged;

    /// <summary>
    ///     播放项改变
    /// </summary>
    public event EventHandler<MusicItemModel>? PlayerItemChanged;
}
