using System;
using System.Linq;
using System.Threading;
using QwQ_Music.Services.Audio.Effect.Base;

namespace QwQ_Music.Services.Audio.Effect;

/// <summary>
/// 环绕效果器
/// </summary>
public sealed class RotatingEffect : AudioEffectBase
{
    private float _rotationSpeed; // 旋转速度（圈/秒）
    private bool _isClockwise; // 是否顺时针旋转
    private float _radius; // 旋转半径（0 到 100 米）
    private float _currentAngle; // 当前角度
    private float[] _channelWeights = null!; // 声道权重
    private readonly Lock _lock = new(); // 线程安全锁
    private float[] _filterStates = null!; // 滤波器状态
    private float _cutoffFrequency; // 当前滤波频率
    private const float MaxCutoff = 20000f; // 最大截止频率（0米时）
    private const float MinCutoff = 1000f; // 最小截止频率（100米时）

    public override string Name => "Rotating";

    protected override void OnInitialized()
    {
        base.OnInitialized();
        int channels = Source.WaveFormat.Channels;

        _channelWeights = new float[channels];
        _filterStates = new float[channels];

        _currentAngle = 0;
        _rotationSpeed = 0.5f;
        _isClockwise = true;
        _radius = 10;
        UpdateCutoffFrequency();
    }

    /// <summary>
    /// 更新截止频率（根据半径）
    /// </summary>
    private void UpdateCutoffFrequency()
    {
        _cutoffFrequency = MathF.Max(MinCutoff, MaxCutoff - _radius / 100f * (MaxCutoff - MinCutoff));
    }

    /// <summary>
    /// 更新声道权重（考虑半径影响）
    /// </summary>
    private void UpdateChannelWeights()
    {
        int channels = Source.WaveFormat.Channels;
        float[] targetWeights = new float[channels];

        // 计算基础权重
        for (int i = 0; i < channels; i++)
        {
            float channelAngle = GetChannelAngle(i, channels);
            float deltaAngle = NormalizeAngle(channelAngle - _currentAngle);

            // 使用高斯分布模拟声场宽度
            float angleFactor = MathF.Exp(-MathF.Pow(deltaAngle / 45f, 2)); // 高斯分布
            targetWeights[i] = MathF.Max(0, angleFactor);
        }

        // 应用半径对声场宽度的影响
        float widthFactor = 1.0f - _radius / 100f * 0.8f; // 保留最小20%的宽度
        for (int i = 0; i < channels; i++)
        {
            targetWeights[i] = targetWeights[i] * widthFactor + (1 - widthFactor) / channels;
        }

        // 归一化处理
        float maxWeight = targetWeights.Max();
        if (maxWeight > 0)
        {
            for (int i = 0; i < channels; i++)
            {
                targetWeights[i] /= maxWeight;
            }
        }

        // 平滑过渡
        const float smooth = 0.2f;
        for (int i = 0; i < channels; i++)
        {
            _channelWeights[i] = Lerp(_channelWeights[i], targetWeights[i], smooth);
        }
    }

    /// <summary>
    /// 线性插值（Lerp）
    /// </summary>
    private static float Lerp(float start, float end, float amount)
    {
        return start + (end - start) * amount;
    }

    /// <summary>
    /// 获取声道的角度（基于声道布局）
    /// </summary>
    private static float GetChannelAngle(int channelIndex, int totalChannels)
    {
        // 假设声道布局为均匀分布的圆周
        return 360f / totalChannels * channelIndex;
    }

    /// <summary>
    /// 角度归一化到 [-180, 180]
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
    /// 读取音频数据并应用环绕音效
    /// </summary>
    public override int Read(float[] buffer, int offset, int count)
    {
        if (!Enabled)
            return Source.Read(buffer, offset, count);

        int samplesRead = Source.Read(buffer, offset, count);

        lock (_lock)
        {
            // 更新角度
            float deltaTime = count / (float)(Source.WaveFormat.SampleRate * Source.WaveFormat.Channels);
            _currentAngle = NormalizeAngle(_currentAngle + _rotationSpeed * 360f * deltaTime * (_isClockwise ? 1 : -1));

            UpdateCutoffFrequency();
            UpdateChannelWeights();

            // 计算滤波器参数
            float rc = 1.0f / (2 * MathF.PI * _cutoffFrequency);
            float dt = 1.0f / Source.WaveFormat.SampleRate;
            float a = dt / (rc + dt);

            // 处理每个采样
            int channels = Source.WaveFormat.Channels;
            for (int n = 0; n < samplesRead; n += channels)
            {
                for (int c = 0; c < channels; c++)
                {
                    int index = offset + n + c;

                    // 应用声场权重
                    buffer[index] *= _channelWeights[c];

                    // 应用低通滤波（模拟距离感）
                    float input = buffer[index];
                    _filterStates[c] = a * input + (1 - a) * _filterStates[c];
                    buffer[index] = _filterStates[c];
                }
            }
        }
        return samplesRead;
    }

    /// <summary>
    /// 克隆当前效果器的配置
    /// </summary>
    public override IAudioEffect Clone()
    {
        var clone = new RotatingEffect();
        clone.SetParameter("RotationSpeed", _rotationSpeed);
        clone.SetParameter("IsClockwise", _isClockwise);
        clone.SetParameter("Radius", _radius);
        clone.Enabled = Enabled;
        clone.Priority = Priority;
        return clone;
    }

    /// <summary>
    /// 设置效果器的配置参数
    /// </summary>
    public override void SetParameter<T>(string key, T value)
    {
        base.SetParameter(key, value);
        switch (key.ToLower())
        {
            case "rotationspeed":
                if (value is float speed)
                {
                    lock (_lock)
                    {
                        _rotationSpeed = Math.Clamp(speed, 0f, 1f);
                    }
                }
                break;

            case "isclockwise":
                if (value is bool clockwise)
                {
                    lock (_lock)
                    {
                        _isClockwise = clockwise;
                    }
                }
                break;

            case "radius":
                if (value is float radius)
                {
                    lock (_lock)
                    {
                        _radius = Math.Clamp(radius, 0f, 100f);
                        UpdateCutoffFrequency();
                    }
                }
                break;
        }
    }

    /// <summary>
    /// 获取效果器的配置参数
    /// </summary>
    public override T GetParameter<T>(string key)
    {
        switch (key.ToLower())
        {
            case "rotationspeed":
                return (T)(object)_rotationSpeed;

            case "isclockwise":
                return (T)(object)_isClockwise;

            case "radius":
                return (T)(object)_radius;

            default:
                return base.GetParameter<T>(key);
        }
    }
}
