using System;
using System.Threading;
using QwQ_Music.Services.Audio.Effect.Base;

namespace QwQ_Music.Services.Audio.Effect;

/// <summary>
/// 音量控制效果器
/// </summary>
public class VolumeEffect : AudioEffectBase
{
    // 原子参数更新
    private volatile VolumeParameters _currentParams = new();
    private VolumeParameters _nextParams = new();

    public override string Name => "Volume";

    protected override void OnInitialized()
    {
        base.OnInitialized();
        SetParameter("Volume", 1.0f); // 默认音量
    }

    /// <summary>
    /// 音频处理核心逻辑
    /// </summary>
    public override int Read(float[] buffer, int offset, int count)
    {
        if (!Enabled)
            return Source.Read(buffer, offset, count);

        var paramsCopy = _currentParams; // 原子读取<button class="citation-flag" data-index="1"><button class="citation-flag" data-index="7">
        int samplesRead = Source.Read(buffer, offset, count);

        // 预计算音量系数（提升SIMD兼容性）
        float volume = paramsCopy.Volume;

        // 向量化处理（自动优化）<button class="citation-flag" data-index="3">
        for (int i = 0; i < samplesRead; i++)
        {
            buffer[offset + i] *= volume;
        }

        return samplesRead;
    }

    /// <summary>
    /// 参数原子更新
    /// </summary>
    public override void SetParameter<T>(string key, T value)
    {
        base.SetParameter(key, value);

        _nextParams = _currentParams.Clone();
        switch (key.ToLower())
        {
            case "volume":
                _nextParams.Volume = ValidateVolume(Convert.ToSingle(value));
                break;
        }
        Interlocked.Exchange(ref _currentParams, _nextParams); // 原子替换<button class="citation-flag" data-index="2"><button class="citation-flag" data-index="8">
    }

    /// <summary>
    /// 参数验证
    /// </summary>
    private float ValidateVolume(float value) => Math.Clamp(value, 0f, 1f);

    /// <summary>
    /// 深度克隆
    /// </summary>
    public override IAudioEffect Clone()
    {
        var clone = new VolumeEffect
        {
            _currentParams = _currentParams.Clone(),
            _nextParams = _nextParams.Clone(),
            Enabled = Enabled,
            Priority = Priority,
        };
        return clone;
    }

    /// <summary>
    /// 参数存储结构
    /// </summary>
    [Serializable]
    private class VolumeParameters : ICloneable
    {
        public float Volume { get; set; }

        public VolumeParameters Clone()
        {
            return new VolumeParameters { Volume = Volume };
        }

        object ICloneable.Clone() => Clone();
    }
}
