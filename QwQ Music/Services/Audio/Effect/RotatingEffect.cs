using System;
using System.Linq;
using System.Threading;
using QwQ_Music.Services.Audio.Effect.Base;

namespace QwQ_Music.Services.Audio.Effect;

/// <summary>
/// 空间环绕效果器
/// </summary>
public sealed class RotatingEffect : AudioEffectBase
{
    // 参数存储（原子更新）
    private volatile RotatingParameters _params = new();

    // 状态变量
    private float _currentAngle;
    private float[] _channelWeights = null!;
    private float[] _filterStates = null!;

    public override string Name => "Rotating";

    protected override void OnInitialized()
    {
        base.OnInitialized();
        int channels = Source.WaveFormat.Channels;
        _channelWeights = new float[channels];
        _filterStates = new float[channels];
        ResetParameters();
    }

    /// <summary>
    /// 核心音频处理逻辑
    /// </summary>
    public override int Read(float[] buffer, int offset, int count)
    {
        if (!Enabled)
            return Source.Read(buffer, offset, count);

        var paramsCopy = _params;
        int samplesRead = Source.Read(buffer, offset, count);
        int channels = Source.WaveFormat.Channels;
        int sampleRate = Source.WaveFormat.SampleRate;
        float dt = 1f / sampleRate;

        // 计算滤波系数
        float rc = 1f / (2 * MathF.PI * paramsCopy.CutoffFrequency);
        float a = dt / (rc + dt);
        float angleIncrement = paramsCopy.RotationSpeed * 360f * dt;
        if (!paramsCopy.IsClockwise)
            angleIncrement *= -1;

        for (int n = 0; n < samplesRead; n += channels)
        {
            _currentAngle = NormalizeAngle(_currentAngle + angleIncrement);

            CalculateChannelWeights(paramsCopy, channels);

            for (int c = 0; c < channels; c++)
            {
                int index = offset + n + c;
                float sample = buffer[index];

                // 应用平滑后的权重
                sample *= _channelWeights[c];

                // 低通滤波
                _filterStates[c] = a * sample + (1 - a) * _filterStates[c];
                buffer[index] = _filterStates[c];
            }
        }

        return samplesRead;
    }

    /// <summary>
    /// 计算声道权重（矢量化优化）
    /// </summary>
    private void CalculateChannelWeights(RotatingParameters parameters, int channels)
    {
        float widthFactor = 1f - parameters.Radius / 100f * 0.8f;
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
    /// 参数更新（原子操作）
    /// </summary>
    public override void SetParameter<T>(string key, T value)
    {
        base.SetParameter(key, value);
        var newParams = _params.Clone();

        switch (key.ToLower())
        {
            case "rotationspeed":
                newParams.RotationSpeed = ValidateSpeed(Convert.ToSingle(value));
                break;
            case "isclockwise":
                newParams.IsClockwise = Convert.ToBoolean(value);
                break;
            case "radius":
                newParams.Radius = ValidateRadius(Convert.ToSingle(value));
                newParams.CutoffFrequency = CalculateCutoff(newParams.Radius);
                break;
        }

        // 原子更新参数
        Interlocked.Exchange(ref _params, newParams);
    }

    /// <summary>
    /// 参数验证
    /// </summary>
    private float ValidateSpeed(float value) => Math.Clamp(value, 0f, 10f);

    private float ValidateRadius(float value) => Math.Clamp(value, 0f, 100f);

    private float CalculateCutoff(float radius) => Lerp(20000f, 1000f, radius / 100f);

    /// <summary>
    /// 线性插值（Lerp）
    /// </summary>
    private static float Lerp(float start, float end, float amount)
    {
        return start + (end - start) * amount;
    }

    /// <summary>
    /// 重置参数到默认值
    /// </summary>
    private void ResetParameters()
    {
        _params = new RotatingParameters
        {
            RotationSpeed = 0.5f,
            IsClockwise = true,
            Radius = 10f,
            CutoffFrequency = CalculateCutoff(10f),
        };
    }

    /// <summary>
    /// 深度克隆
    /// </summary>
    public override IAudioEffect Clone()
    {
        var clone = new RotatingEffect
        {
            _params = _params.Clone(),
            _currentAngle = _currentAngle,
            Enabled = Enabled,
            Priority = Priority,
        };
        return clone;
    }

    /// <summary>
    /// 参数存储结构
    /// </summary>
    [Serializable]
    private class RotatingParameters : ICloneable
    {
        public float RotationSpeed;
        public bool IsClockwise;
        public float Radius;
        public float CutoffFrequency;

        public RotatingParameters Clone()
        {
            return new RotatingParameters
            {
                RotationSpeed = RotationSpeed,
                IsClockwise = IsClockwise,
                Radius = Radius,
                CutoffFrequency = CutoffFrequency,
            };
        }

        object ICloneable.Clone() => Clone();
    }
}
