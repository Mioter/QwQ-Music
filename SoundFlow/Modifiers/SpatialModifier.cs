using System;
using SoundFlow.Abstracts;

namespace SoundFlow.Modifiers;

/// <summary>
/// 空间定位效果器<br />
/// Spatial positioning effect modifier
/// </summary>
public sealed class SpatialModifier : SoundModifier
{
    // 状态变量
    private float _currentAngle;
    private float _currentDistance;
    private float[] _channelWeights = [];
    
    // 配置参数
    private float _angle;
    private float _distance = 10f;

    /// <inheritdoc />
    public override string Name { get; set; } = "Spatial Positioning";

    /// <summary>
    /// 声源角度 (-180 到 180 度)<br />
    /// Sound source angle (-180 to 180 degrees)
    /// </summary>
    public float Angle
    {
        get => _angle;
        set => _angle = NormalizeAngle(value);
    }

    /// <summary>
    /// 声源距离 (0 到 100)<br />
    /// Sound source distance (0 to 100)
    /// </summary>
    public float Distance
    {
        get => _distance;
        set => _distance = Math.Clamp(value, 0f, 100f);
    }

    /// <summary>
    /// 构造函数<br />
    /// Constructor
    /// </summary>
    public SpatialModifier()
    {
        Initialize();
    }

    private void Initialize()
    {
        int channels = AudioEngine.Channels;
        _channelWeights = new float[channels];
    }

    /// <inheritdoc />
    public override void Process(Span<float> buffer)
    {
        int channels = AudioEngine.Channels;
        
        // 确保通道权重数组大小正确
        if (_channelWeights.Length != channels)
        {
            _channelWeights = new float[channels];
        }

        // 预计算音量衰减
        float volumeScale = MathF.Pow(0.1f, _distance / 100f);
        
        // 平滑更新参数
        UpdateSpatialParameters();
        
        // 计算声道权重
        CalculateChannelWeights(channels);

        for (int i = 0; i < buffer.Length; i++)
        {
            int channel = i % channels;
            buffer[i] *= _channelWeights[channel] * volumeScale;
        }
    }

    /// <inheritdoc />
    public override float ProcessSample(float sample, int channel)
    {
        if (!Enabled) return sample;
        
        // 确保通道权重数组大小正确
        if (_channelWeights.Length <= channel)
        {
            Array.Resize(ref _channelWeights, channel + 1);
        }
        
        // 预计算音量衰减
        float volumeScale = MathF.Pow(0.1f, _distance / 100f);
        
        // 平滑更新参数
        UpdateSpatialParameters();
        
        // 计算声道权重
        CalculateChannelWeights(_channelWeights.Length);
        
        return sample * _channelWeights[channel] * volumeScale;
    }

    /// <summary>
    /// 参数平滑更新<br />
    /// Smooth parameter update
    /// </summary>
    private void UpdateSpatialParameters()
    {
        const float smoothFactor = 0.15f;

        // 角度插值（带相位修正）
        float deltaAngle = _angle - _currentAngle;
        if (MathF.Abs(deltaAngle) > 180)
        {
            deltaAngle -= MathF.Sign(deltaAngle) * 360;
        }
        _currentAngle += deltaAngle * smoothFactor;

        // 距离插值
        _currentDistance += (_distance - _currentDistance) * smoothFactor;
    }

    /// <summary>
    /// 计算声道权重<br />
    /// Calculate channel weights
    /// </summary>
    private void CalculateChannelWeights(int channels)
    {
        float maxWeight = 0f;
        float baseWeight = 0.2f;

        for (int c = 0; c < channels; c++)
        {
            float channelAngle = GetChannelAngle(c, channels);
            float delta = MathF.Abs(_currentAngle - channelAngle);
            delta = MathF.Min(delta, 360f - delta);

            // 主方向权重（余弦平方）
            float mainWeight = MathF.Pow(MathF.Cos(delta * MathF.PI / 360f), 2);

            // 环境权重（高斯分布）
            float ambientWeight = 0.3f * MathF.Exp(-MathF.Pow(delta / 180f, 2));

            // 合成权重
            float weight = Math.Clamp(mainWeight + ambientWeight + baseWeight, 0.1f, 1f);
            _channelWeights[c] = weight;
            maxWeight = MathF.Max(maxWeight, weight);
        }

        // 能量归一化
        if (maxWeight > 0)
        {
            for (int c = 0; c < channels; c++)
            {
                _channelWeights[c] /= maxWeight;
            }
        }
    }

    /// <summary>
    /// 获取声道的角度（基于声道布局）<br />
    /// Get channel angle based on channel layout
    /// </summary>
    private static float GetChannelAngle(int channelIndex, int totalChannels)
    {
        // 四声道布局 (FL, FR, RL, RR)
        if (totalChannels == 4)
        {
            return channelIndex switch
            {
                0 => 135f, // 右前 (Front Left)
                1 => -135f, // 左前 (Front Right)
                2 => -45f, // 右后 (Rear Left)
                3 => 45f, // 左后 (Rear Right)
                _ => 0f,
            };
        }

        // 立体声布局
        if (totalChannels == 2)
        {
            return channelIndex switch
            {
                0 => -90f, // 左声道
                1 => 90f, // 右声道
                _ => 0f,
            };
        }

        return 360f / totalChannels * channelIndex;
    }

    /// <summary>
    /// 角度归一化<br />
    /// Normalize angle to [-180, 180]
    /// </summary>
    private static float NormalizeAngle(float angle)
    {
        angle %= 360f;
        return angle > 180 ? angle - 360 : angle;
    }
}