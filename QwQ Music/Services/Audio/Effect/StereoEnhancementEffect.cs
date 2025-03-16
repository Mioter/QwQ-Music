using System;
using System.Threading;
using QwQ_Music.Services.Audio.Effect.Base;

namespace QwQ_Music.Services.Audio.Effect;

/// <summary>
/// 立体声增强效果器
/// </summary>
public class StereoEnhancementEffect : AudioEffectBase
{
    // 原子参数更新
    private volatile StereoParameters _currentParams = new();
    private StereoParameters _nextParams = new();

    // 滤波器状态（线程安全）
    private float[] _bassFilterStates = new float[2];

    // 预计算系数
    private float _sampleTime;

    public override string Name => "Stereo Enhancement";

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _sampleTime = 1f / WaveFormat.SampleRate;

        // 初始化默认参数
        SetParameter("EnhancementFactor", 1.5f);
        SetParameter("StereoWidth", 1.0f);
        SetParameter("BassMixing", false);
        SetParameter("HighFrequencyBoost", 1.0f);
        SetParameter("DynamicRangeCompression", 0.0f);
    }

    /// <summary>
    /// 音频处理核心逻辑
    /// </summary>
    public override int Read(float[] buffer, int offset, int count)
    {
        if (!Enabled)
            return Source.Read(buffer, offset, count);

        var paramsCopy = _currentParams;
        int samplesRead = Source.Read(buffer, offset, count);
        int channels = WaveFormat.Channels;

        for (int n = 0; n < samplesRead; n += channels)
        {
            // 获取左右声道样本
            int leftIndex = offset + n;
            int rightIndex = leftIndex + 1;
            float left = buffer[leftIndex];
            float right = buffer[rightIndex];

            // 计算中间/侧边信号
            float mid = (left + right) * 0.5f;
            float side = (left - right) * paramsCopy.EnhancementFactor;

            // 应用立体声宽度
            side *= paramsCopy.StereoWidth;

            // 重建左右声道
            float newLeft = mid + side;
            float newRight = mid - side;

            // 低频混合处理
            if (paramsCopy.BassMixing)
            {
                float bassLeft = ProcessBassFilter(left, 0, paramsCopy);
                float bassRight = ProcessBassFilter(right, 1, paramsCopy);

                newLeft = (newLeft + bassLeft) * 0.5f;
                newRight = (newRight + bassRight) * 0.5f;
            }

            // 高频增强
            newLeft *= paramsCopy.HighFrequencyBoost;
            newRight *= paramsCopy.HighFrequencyBoost;

            // 动态范围压缩
            float maxAmp = MathF.Max(MathF.Abs(newLeft), MathF.Abs(newRight));
            if (maxAmp > 1f && paramsCopy.CompressionFactor > 0f)
            {
                float comp = 1f / (1f + paramsCopy.CompressionFactor * maxAmp);
                newLeft *= comp;
                newRight *= comp;
            }

            // 软限幅
            buffer[leftIndex] = SoftClip(newLeft);
            buffer[rightIndex] = SoftClip(newRight);
        }

        return samplesRead;
    }

    /// <summary>
    /// 软限幅函数
    /// </summary>
    private static float SoftClip(float sample, float threshold = 0.9f)
    {
        if (Math.Abs(sample) <= threshold)
            return sample;
        return Math.Sign(sample)
            * (threshold + (1 - threshold) * MathF.Tanh((Math.Abs(sample) - threshold) / (1 - threshold)));
    }

    /// <summary>
    /// 低通滤波器处理
    /// </summary>
    private float ProcessBassFilter(float input, int channel, StereoParameters paramsCopy)
    {
        float alpha = paramsCopy.BassFilterCoeff * _sampleTime;
        _bassFilterStates[channel] = alpha * input + (1 - alpha) * _bassFilterStates[channel];
        return _bassFilterStates[channel];
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
            case "enhancementfactor":
                _nextParams.EnhancementFactor = ValidateFactor(Convert.ToSingle(value));
                break;
            case "stereowidth":
                _nextParams.StereoWidth = ValidateWidth(Convert.ToSingle(value));
                break;
            case "bassmixing":
                _nextParams.BassMixing = Convert.ToBoolean(value);
                break;
            case "highfrequencyboost":
                _nextParams.HighFrequencyBoost = ValidateBoost(Convert.ToSingle(value));
                break;
            case "dynamicrangecompression":
                _nextParams.DynamicRangeCompression = ValidateCompression(Convert.ToSingle(value));
                break;
        }
        Interlocked.Exchange(ref _currentParams, _nextParams);
    }

    /// <summary>
    /// 参数验证
    /// </summary>
    private static float ValidateFactor(float value) => Math.Clamp(value, 0.5f, 3.0f);

    private static float ValidateWidth(float value) => Math.Clamp(value, 0.0f, 2.0f);

    private static float ValidateBoost(float value) => Math.Clamp(value, 0.0f, 2.0f);

    private static float ValidateCompression(float value) => Math.Clamp(value, 0.0f, 1.0f);

    /// <summary>
    /// 深度克隆
    /// </summary>
    public override IAudioEffect Clone()
    {
        var clone = new StereoEnhancementEffect
        {
            _currentParams = _currentParams.Clone(),
            _nextParams = _nextParams.Clone(),
            _bassFilterStates = (float[])_bassFilterStates.Clone(),
            Enabled = Enabled,
            Priority = Priority,
        };
        return clone;
    }

    /// <summary>
    /// 参数存储结构
    /// </summary>
    [Serializable]
    private class StereoParameters : ICloneable
    {
        public float EnhancementFactor { get; set; }
        public float StereoWidth { get; set; }
        public bool BassMixing { get; set; }
        public float HighFrequencyBoost { get; set; }
        public float DynamicRangeCompression { get; set; }
        public float BassFilterCoeff => 2 * MathF.PI * 200f; // 200Hz截止频率
        public float CompressionFactor => DynamicRangeCompression * 0.5f;

        public StereoParameters Clone()
        {
            return new StereoParameters
            {
                EnhancementFactor = EnhancementFactor,
                StereoWidth = StereoWidth,
                BassMixing = BassMixing,
                HighFrequencyBoost = HighFrequencyBoost,
                DynamicRangeCompression = DynamicRangeCompression,
            };
        }

        object ICloneable.Clone() => Clone();
    }
}
