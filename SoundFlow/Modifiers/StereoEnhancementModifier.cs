using SoundFlow.Abstracts;

namespace SoundFlow.Modifiers;

/// <summary>
/// 立体声增强效果器<br />
/// Stereo enhancement effect modifier
/// </summary>
public sealed class StereoEnhancementModifier : SoundModifier
{
    // 状态变量 - 使用更精确的初始化方式
    private readonly float[] _bassFilterStates = new float[2];
    private readonly float[] _previousSamples = new float[2]; // 添加前一样本缓存用于相位校正

    // 配置参数
    private float _enhancementFactor = 1.5f;
    private float _stereoWidth = 1.0f;
    private float _highFrequencyBoost = 1.0f;
    private float _dynamicRangeCompression;
    private float _crossfeedAmount; // 添加交叉反馈参数

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
    public bool BassMixing { get; set; }

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
    /// 交叉反馈量 (0.0-0.5)<br />
    /// Crossfeed amount (0.0-0.5)
    /// </summary>
    public float CrossfeedAmount
    {
        get => _crossfeedAmount;
        set => _crossfeedAmount = Math.Clamp(value, 0.0f, 0.5f);
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
    private const float BASS_FILTER_FREQ = 200f; // 200Hz截止频率

    /// <inheritdoc />
    public override void Process(Span<float> buffer)
    {
        if (!Enabled)
            return;

        int channels = AudioEngine.Channels;

        // 只处理立体声
        if (channels < 2)
            return;

        float sampleTime = AudioEngine.Instance.InverseSampleRate;
        float bassFilterCoeff = 2 * MathF.PI * BASS_FILTER_FREQ;
        float alpha = bassFilterCoeff * sampleTime;
        float alphaComplement = 1 - alpha;

        for (int n = 0; n < buffer.Length; n += channels)
        {
            // 确保有足够的样本
            if (n + 1 >= buffer.Length)
                break;

            // 获取左右声道样本
            float left = buffer[n];
            float right = buffer[n + 1];

            // 应用交叉反馈（改善声像定位）
            if (_crossfeedAmount > 0)
            {
                float leftMix = left * (1 - _crossfeedAmount) + right * _crossfeedAmount;
                float rightMix = right * (1 - _crossfeedAmount) + left * _crossfeedAmount;
                left = leftMix;
                right = rightMix;
            }

            // 计算中间/侧边信号
            float mid = (left + right) * 0.5f;
            float side = (left - right) * _enhancementFactor;

            // 应用立体声宽度
            side *= _stereoWidth;

            // 重建左右声道
            float newLeft = mid + side;
            float newRight = mid - side;

            // 相位校正（减少相位失真）
            newLeft = 0.97f * newLeft + 0.03f * _previousSamples[0];
            newRight = 0.97f * newRight + 0.03f * _previousSamples[1];
            _previousSamples[0] = left;
            _previousSamples[1] = right;

            // 低频混合处理 - 优化计算
            if (BassMixing)
            {
                // 内联低通滤波器计算以提高性能
                _bassFilterStates[0] = alpha * left + alphaComplement * _bassFilterStates[0];
                _bassFilterStates[1] = alpha * right + alphaComplement * _bassFilterStates[1];

                // 混合低频内容
                newLeft = newLeft * 0.7f + _bassFilterStates[0] * 0.3f;
                newRight = newRight * 0.7f + _bassFilterStates[1] * 0.3f;
            }

            // 高频增强 - 仅当需要时应用
            if (_highFrequencyBoost != 1.0f)
            {
                newLeft *= _highFrequencyBoost;
                newRight *= _highFrequencyBoost;
            }

            // 动态范围压缩 - 优化条件检查
            float compressionFactor = CompressionFactor;
            if (compressionFactor > 0f)
            {
                float maxAmp = MathF.Max(MathF.Abs(newLeft), MathF.Abs(newRight));
                if (maxAmp > 1f)
                {
                    float comp = 1f / (1f + compressionFactor * (maxAmp - 1f));
                    newLeft *= comp;
                    newRight *= comp;
                }
            }

            // 软限幅 - 使用更高效的实现
            buffer[n] = FastSoftClip(newLeft);
            buffer[n + 1] = FastSoftClip(newRight);
        }
    }

    /// <inheritdoc />
    public override float ProcessSample(float sample, int channel)
    {
        // 单样本处理在Process中已实现，这里仅返回原样本
        return sample;
    }

    /// <summary>
    /// 优化的软限幅函数<br />
    /// Optimized soft clipping function
    /// </summary>
    private static float FastSoftClip(float sample, float threshold = 0.9f)
    {
        float absSample = MathF.Abs(sample);
        if (absSample <= threshold)
            return sample;

        // 使用更高效的软限幅算法
        float sign = MathF.Sign(sample);
        float delta = absSample - threshold;
        float scale = 1 - threshold;
        return sign * (threshold + scale * (1 - MathF.Exp(-delta / scale)));
    }
}
