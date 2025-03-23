using System;
using System.Linq;
using SoundFlow.Abstracts;

namespace SoundFlow.Modifiers;

/// <summary>
/// 空间环绕效果器<br />
/// Rotating spatial effect modifier
/// </summary>
public sealed class RotatingModifier : SoundModifier
{
    // 状态变量
    private float _currentAngle;
    private float[] _channelWeights = [];
    private float[] _filterStates = [];
    
    // 配置参数
    private float _rotationSpeed = 0.5f;
    private float _radius = 10f;
    private float _cutoffFrequency;

    /// <inheritdoc />
    public override string Name { get; set; } = "Rotating Spatial Effect";

    /// <summary>
    /// 旋转速度 (0-10)<br />
    /// Rotation speed (0-10)
    /// </summary>
    public float RotationSpeed
    {
        get => _rotationSpeed;
        set => _rotationSpeed = Math.Clamp(value, 0f, 10f);
    }

    /// <summary>
    /// 是否顺时针旋转<br />
    /// Whether to rotate clockwise
    /// </summary>
    public bool IsClockwise { get; set; } = true;

    /// <summary>
    /// 旋转半径 (0-100)<br />
    /// Rotation radius (0-100)
    /// </summary>
    public float Radius
    {
        get => _radius;
        set
        {
            _radius = Math.Clamp(value, 0f, 100f);
            _cutoffFrequency = CalculateCutoff(_radius);
        }
    }

    /// <summary>
    /// 构造函数<br />
    /// Constructor
    /// </summary>
    public RotatingModifier()
    {
        int channels = AudioEngine.Channels;
        _channelWeights = new float[channels];
        _filterStates = new float[channels];
        _cutoffFrequency = CalculateCutoff(_radius);
    }

    /// <inheritdoc />
    public override void Process(Span<float> buffer)
    {
        int channels = AudioEngine.Channels;
        int sampleRate = AudioEngine.Instance.SampleRate;
        float dt = 1f / sampleRate;

        // 计算滤波系数
        float rc = 1f / (2 * MathF.PI * _cutoffFrequency);
        float a = dt / (rc + dt);
        float angleIncrement = _rotationSpeed * 360f * dt;
        if (!IsClockwise)
            angleIncrement *= -1;

        // 确保通道权重和滤波状态数组大小正确
        if (_channelWeights.Length != channels || _filterStates.Length != channels)
        {
            _channelWeights = new float[channels];
            _filterStates = new float[channels];
        }

        for (int n = 0; n < buffer.Length; n += channels)
        {
            _currentAngle = NormalizeAngle(_currentAngle + angleIncrement);

            CalculateChannelWeights(channels);

            for (int c = 0; c < channels && n + c < buffer.Length; c++)
            {
                int index = n + c;
                float sample = buffer[index];

                // 应用平滑后的权重
                sample *= _channelWeights[c];

                // 低通滤波
                _filterStates[c] = a * sample + (1 - a) * _filterStates[c];
                buffer[index] = _filterStates[c];
            }
        }
    }

    /// <inheritdoc />
    public override float ProcessSample(float sample, int channel)
    {
        // 单样本处理在Process中已实现，这里仅返回原样本
        return sample;
    }

    /// <summary>
    /// 计算声道权重<br />
    /// Calculate channel weights
    /// </summary>
    private void CalculateChannelWeights(int channels)
    {
        float widthFactor = 1f - _radius / 100f * 0.8f;
        const float smooth = 0.2f;

        for (int c = 0; c < channels; c++)
        {
            float channelAngle = 360f / channels * c;
            float delta = NormalizeAngle(_currentAngle - channelAngle);

            // 高斯分布权重
            float targetWeight = MathF.Exp(-MathF.Pow(delta / 45f, 2));
            targetWeight = targetWeight * widthFactor + (1 - widthFactor) / channels;

            // 平滑过渡
            _channelWeights[c] = Lerp(_channelWeights[c], targetWeight, smooth);
        }

        // 归一化处理
        float maxWeight = _channelWeights.Max();
        if (maxWeight > 0)
        {
            for (int c = 0; c < channels; c++)
            {
                _channelWeights[c] /= maxWeight;
            }
        }
    }

    /// <summary>
    /// 角度归一化到 [-180, 180]<br />
    /// Normalize angle to [-180, 180]
    /// </summary>
    private static float NormalizeAngle(float angle)
    {
        while (angle > 180)
            angle -= 360;
        while (angle < -180)
            angle += 360;
        return angle;
    }

    /// <summary>
    /// 计算截止频率<br />
    /// Calculate cutoff frequency
    /// </summary>
    private static float CalculateCutoff(float radius)
    {
        return Lerp(20000f, 1000f, radius / 100f);
    }

    /// <summary>
    /// 线性插值<br />
    /// Linear interpolation
    /// </summary>
    private static float Lerp(float start, float end, float amount)
    {
        return start + (end - start) * amount;
    }
}