using System;
using System.Threading;
using QwQ_Music.Services.Audio.Effect.Base;

namespace QwQ_Music.Services.Audio.Effect;

/// <summary>
/// 空间定位效果器
/// </summary>
public sealed class SpatialEffect : AudioEffectBase
{
    // 参数存储（原子更新）
    private volatile SpatialParameters _params = new();

    // 状态变量
    private float _currentAngle;
    private float _currentDistance;
    private float[] _channelWeights = null!;

    public override string Name => "Spatial";

    protected override void OnInitialized()
    {
        base.OnInitialized();
        int channels = Source.WaveFormat.Channels;
        _channelWeights = new float[channels];
        ResetParameters();
    }

    /// <summary>
    /// 核心音频处理逻辑
    /// </summary>
    public override int Read(float[] buffer, int offset, int count)
    {
        if (!Enabled)
            return Source.Read(buffer, offset, count);

        var paramsCopy = _params; // 原子读取参数
        int samplesRead = Source.Read(buffer, offset, count);
        int channels = Source.WaveFormat.Channels;

        // 预计算音量衰减
        float volumeScale = MathF.Pow(0.1f, paramsCopy.Distance / 100f);

        // 处理音频块
        for (int n = 0; n < samplesRead; n += channels)
        {
            // 更新空间参数
            UpdateSpatialParameters(paramsCopy);

            // 计算声道权重
            CalculateChannelWeights(paramsCopy, channels);

            // 应用空间效果
            for (int c = 0; c < channels; c++)
            {
                int index = offset + n + c;
                buffer[index] *= _channelWeights[c] * volumeScale;
            }
        }

        return samplesRead;
    }

    /// <summary>
    /// 参数平滑更新（SIMD优化）
    /// </summary>
    private void UpdateSpatialParameters(SpatialParameters target)
    {
        const float smoothFactor = 0.15f;

        // 角度插值（带相位修正）
        float deltaAngle = target.Angle - _currentAngle;
        if (MathF.Abs(deltaAngle) > 180)
        {
            deltaAngle -= MathF.Sign(deltaAngle) * 360;
        }
        _currentAngle += deltaAngle * smoothFactor;

        // 距离插值
        _currentDistance += (target.Distance - _currentDistance) * smoothFactor;
    }

    /// <summary>
    /// 计算声道权重（矢量化优化）
    /// </summary>
    private void CalculateChannelWeights(SpatialParameters parameters, int channels)
    {
        float maxWeight = 0f;
        float baseWeight = 0.2f;

        for (int c = 0; c < channels; c++)
        {
            float channelAngle = GetChannelAngle(c, channels);
            float delta = MathF.Abs(parameters.Angle - channelAngle);
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
    /// 获取声道的角度（基于声道布局）
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
    /// 参数更新（原子操作）
    /// </summary>
    public override void SetParameter<T>(string key, T value)
    {
        base.SetParameter(key, value);
        var newParams = _params.Clone();

        switch (key.ToLower())
        {
            case "angle":
                newParams.Angle = ValidateAngle(Convert.ToSingle(value));
                break;
            case "distance":
                newParams.Distance = ValidateDistance(Convert.ToSingle(value));
                break;
        }

        // 原子更新参数
        Interlocked.Exchange(ref _params, newParams);
    }

    /// <summary>
    /// 参数验证
    /// </summary>
    private float ValidateAngle(float value) => Math.Clamp(NormalizeAngle(value), -180f, 180f);

    private float ValidateDistance(float value) => Math.Clamp(value, 0f, 100f);

    /// <summary>
    /// 角度归一化
    /// </summary>
    private float NormalizeAngle(float angle)
    {
        angle %= 360f;
        return angle > 180 ? angle - 360 : angle;
    }

    /// <summary>
    /// 重置参数到默认值
    /// </summary>
    private void ResetParameters()
    {
        _params = new SpatialParameters { Angle = 0f, Distance = 10f };
    }

    /// <summary>
    /// 深度克隆
    /// </summary>
    public override IAudioEffect Clone()
    {
        var clone = new SpatialEffect
        {
            _params = _params.Clone(),
            Enabled = Enabled,
            Priority = Priority,
        };
        return clone;
    }

    /// <summary>
    /// 参数存储结构
    /// </summary>
    [Serializable]
    private class SpatialParameters : ICloneable
    {
        public float Angle;
        public float Distance;

        public SpatialParameters Clone()
        {
            return new SpatialParameters { Angle = Angle, Distance = Distance };
        }

        object ICloneable.Clone() => Clone();
    }
}
