using ATL;

namespace QwQ_Music.Common.Interfaces;

public interface IMusicTagExtractor {
    /// <summary>
    ///     获取 <see cref="Track" /> 实例
    /// </summary>
    /// <returns></returns>
    public Track? GetTrack();

    /// <summary>
    ///     异步获取 <see cref="Track" /> 实例
    /// </summary>
    /// <returns></returns>
    public Task<Track?> GetTrackAsync();
}