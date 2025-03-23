using System;
using SoundFlow.Abstracts;

namespace SoundFlow.Modifiers;

/// <summary>
/// 立体声增强效果器<br />
/// Stereo enhancement effect modifier
/// </summary>
public sealed class StereoEnhancementModifier : SoundModifier
{
    // 状态变量
    private readonly float[] _bassFilterStates = new float[2];
    
    // 配置参数
    private float _enhancementFactor = 1.5f;
    private float _stereoWidth = 1.0f;
    private float _highFrequencyBoost = 1.0f;
    private float _dynamicRangeCompression = 0.0f;

    /// <inheritdoc />
    public override string Name { get; set; } = "Stereo Enhancement";

    /// <summary>
    /// 增强因子 (0.5-3.0)<br />
    /// Enhancement factor (0.5-3.0)
    /// </summary>
    public float EnhancementFactor
    {
        get => _enhancementFactor;
        set => _enhancementFactor = Math.Clamp(value, 0.5f, 3.0f);
    }

    /// <summary>
    /// 立体声宽度 (0.0-2.0)<br />
    /// Stereo width (0.0-2.0)
    /// </summary>
    public float StereoWidth
    {
        get => _stereoWidth;
        set => _stereoWidth = Math.Clamp(value, 0.0f, 2.0f);
    }

    /// <summary>
    /// 低频混合<br />
    /// Bass mixing
    /// </summary>
    public bool BassMixing { get; set; } = false;

    /// <summary>
    /// 高频增强 (0.0-2.0)<br />
    /// High frequency boost (0.0-2.0)
    /// </summary>
    public float HighFrequencyBoost
    {
        get => _highFrequencyBoost;
        set => _highFrequencyBoost = Math.Clamp(value, 0.0f, 2.0f);
    }

    /// <summary>
    /// 动态范围压缩 (0.0-1.0)<br />
    /// Dynamic range compression (0.0-1.0)
    /// </summary>
    public float DynamicRangeCompression
    {
        get => _dynamicRangeCompression;
        set => _dynamicRangeCompression = Math.Clamp(value, 0.0f, 1.0f);
    }

    /// <summary>
    /// 压缩因子<br />
    /// Compression factor
    /// </summary>
    private float CompressionFactor => _dynamicRangeCompression * 0.5f;

    /// <summary>
    /// 低通滤波器系数<br />
    /// Bass filter coefficient
    /// </summary>
    private static float BassFilterCoeff => 2 * MathF.PI * 200f; // 200Hz截止频率

    /// <summary>
    /// 构造函数<br />
    /// Constructor
    /// </summary>
    public StereoEnhancementModifier()
    {
        _bassFilterStates = new float[2];
    }

    /// <inheritdoc />
    public override void Process(Span<float> buffer)
    {
        if (!Enabled) return;

        int channels = AudioEngine.Channels;
        
        // 只处理立体声
        if (channels < 2) return;

        float sampleTime = 1f / AudioEngine.Instance.SampleRate;

        for (int n = 0; n < buffer.Length; n += channels)
        {
            // 确保有足够的样本
            if (n + 1 >= buffer.Length) break;

            // 获取左右声道样本
            float left = buffer[n];
            float right = buffer[n + 1];

            // 计算中间/侧边信号
            float mid = (left + right) * 0.5f;
            float side = (left - right) * _enhancementFactor;

            // 应用立体声宽度
            side *= _stereoWidth;

            // 重建左右声道
            float newLeft = mid + side;
            float newRight = mid - side;

            // 低频混合处理
            if (BassMixing)
            {
                float bassLeft = ProcessBassFilter(left, 0, sampleTime);
                float bassRight = ProcessBassFilter(right, 1, sampleTime);

                newLeft = (newLeft + bassLeft) * 0.5f;
                newRight = (newRight + bassRight) * 0.5f;
            }

            // 高频增强
            newLeft *= _highFrequencyBoost;
            newRight *= _highFrequencyBoost;

            // 动态范围压缩
            float maxAmp = MathF.Max(MathF.Abs(newLeft), MathF.Abs(newRight));
            if (maxAmp > 1f && CompressionFactor > 0f)
            {
                float comp = 1f / (1f + CompressionFactor * maxAmp);
                newLeft *= comp;
                newRight *= comp;
            }

            // 软限幅
            buffer[n] = SoftClip(newLeft);
            buffer[n + 1] = SoftClip(newRight);
        }
    }

    /// <inheritdoc />
    public override float ProcessSample(float sample, int channel)
    {
        // 单样本处理在Process中已实现，这里仅返回原样本
        return sample;
    }

    /// <summary>
    /// 软限幅函数<br />
    /// Soft clipping function
    /// </summary>
    private static float SoftClip(float sample, float threshold = 0.9f)
    {
        if (Math.Abs(sample) <= threshold)
            return sample;
        
        return Math.Sign(sample) * 
               (threshold + (1 - threshold) * MathF.Tanh((Math.Abs(sample) - threshold) / (1 - threshold)));
    }

    /// <summary>
    /// 低通滤波器处理<br />
    /// Low-pass filter processing
    /// </summary>
    private float ProcessBassFilter(float input, int channel, float sampleTime)
    {
        float alpha = BassFilterCoeff * sampleTime;
        _bassFilterStates[channel] = alpha * input + (1 - alpha) * _bassFilterStates[channel];
        return _bassFilterStates[channel];
    }
}