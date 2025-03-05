using System;
using System.Linq;
using System.Threading;
using QwQ_Music.Services.Audio.Effect.Base;

namespace QwQ_Music.Services.Audio.Effect;

/// <summary>
/// 空间效果器
/// </summary>
public sealed class SpatialEffect : AudioEffectBase
{
    private float _currentAngle; // 当前角度
    private float _targetAngle; // 目标角度（范围：-180 到 180 度）
    private float _currentDistance; // 当前距离
    private float _targetDistance; // 目标距离（范围：0 到 100 米）
    private float[] _channelWeights = null!; // 每个声道的权重
    private readonly Lock _lock = new(); // 确保线程安全

    public override string Name => "Spatial";

    protected override void OnInitialized()
    {
        base.OnInitialized();
        int channels = Source.WaveFormat.Channels;

        // 初始化声道权重数组
        _channelWeights = new float[channels];

        // 设置初始值
        _currentAngle = _targetAngle = 0;
        _currentDistance = _targetDistance = 10;

        UpdateChannelWeights();
    }

    /// <summary>
    /// 读取音频数据并应用空间音效
    /// </summary>
    public override int Read(float[] buffer, int offset, int count)
    {
        if (!Enabled) // 如果效果器未启用，直接返回原始数据
        {
            return Source.Read(buffer, offset, count);
        }

        int samplesRead = Source.Read(buffer, offset, count);

        lock (_lock)
        {
            // 平滑过渡角度和距离
            SmoothTransition();

            // 计算音量缩放因子
            float volumeScale = MathF.Pow(0.1f, _currentDistance / 100f);

            int channels = Source.WaveFormat.Channels;

            for (int n = 0; n < samplesRead; n += channels)
            {
                // 计算每个声道的音量
                for (int c = 0; c < channels; c++)
                {
                    int index = offset + n + c;
                    buffer[index] *= _channelWeights[c] * volumeScale;
                }
            }
        }

        return samplesRead;
    }

    /// <summary>
    /// 平滑过渡角度和距离
    /// </summary>
    private void SmoothTransition()
    {
        const float responsiveFactor = 0.6f; // 高响应性系数

        // 使用二次插值提高响应速度
        _currentAngle += (_targetAngle - _currentAngle) * responsiveFactor;
        _currentDistance += (_targetDistance - _currentDistance) * responsiveFactor;

        // 角度相位修正（防止360°跳变）
        if (Math.Abs(_targetAngle - _currentAngle) > 180f)
        {
            _currentAngle += 360f * Math.Sign(_targetAngle - _currentAngle);
        }

        // 限制数值范围
        _currentAngle = NormalizeAngle(_currentAngle);
        _currentDistance = Math.Clamp(_currentDistance, 0f, 100f);

        UpdateChannelWeights();
    }

    /// <summary>
    /// 更新声道权重
    /// </summary>
    private void UpdateChannelWeights()
    {
        lock (_lock)
        {
            int channels = Source.WaveFormat.Channels;
            float baseWeight = 0.2f; // 基础音量保证

            for (int i = 0; i < channels; i++)
            {
                float channelAngle = GetChannelAngle(i, channels);

                // 1. 计算主方向权重（使用余弦平方）
                float mainWeight = MathF.Pow(
                    MathF.Cos(NormalizeAngle(channelAngle - _currentAngle) * MathF.PI / 360f),
                    2
                );

                // 2. 计算环境扩散权重（全向分量）
                float ambientWeight =
                    0.3f * MathF.Exp(-MathF.Pow(NormalizeAngle(channelAngle - _currentAngle) / 180f, 2));

                // 3. 合成最终权重
                _channelWeights[i] = Math.Clamp(mainWeight + ambientWeight + baseWeight, 0.1f, 1f);
            }

            // 基于能量守恒的归一化
            float weightSum = _channelWeights.Sum();
            for (int i = 0; i < channels; i++)
            {
                _channelWeights[i] /= weightSum;
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

        // 立体声布局保持原有逻辑
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
    /// 克隆当前效果器的配置
    /// </summary>
    public override IAudioEffect Clone()
    {
        var clone = new SpatialEffect();
        clone.SetParameter("TargetAngle", _targetAngle);
        clone.SetParameter("TargetDistance", _targetDistance);
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
            case "angle":
                if (value is float angle)
                {
                    lock (_lock)
                    {
                        _targetAngle = Math.Clamp(angle, -180f, 180f);
                    }
                }
                break;

            case "distance":
                if (value is float distance)
                {
                    lock (_lock)
                    {
                        _targetDistance = Math.Clamp(distance, 0f, 100f);
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
            case "angle":
                return (T)(object)_targetAngle;

            case "distance":
                return (T)(object)_targetDistance;

            default:
                return base.GetParameter<T>(key);
        }
    }
}
