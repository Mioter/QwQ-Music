using System;
using System.Text;
using System.Threading;

namespace QwQ_Music.Services.Effect;

/// <summary>
/// 立体声增强效果器
/// </summary>
public class StereoEnhancementEffect : AudioEffectBase
{    
    private readonly Lock _lock = new(); // 确保线程安全
    
    /// <summary>
    /// 增强因子（范围：0.5 到 3.0）
    /// </summary>
    private float _enhancementFactor = 1.5f; 
   
    /// <summary>
    /// 立体声宽度（范围：0.0 到 2.0）
    /// </summary>
    private float _stereoWidth = 1.0f;  
    
    /// <summary>
    /// 高频增强因子（范围：0.0 到 2.0）
    /// </summary>
    private float _highFrequencyBoost = 1.0f; 
    
    /// <summary>
    /// 动态范围压缩（范围：0.0 到 1.0）
    /// </summary>
    private float _dynamicRangeCompression = 0.0f;
        
    /// <summary>
    /// 是否混合低频信号
    /// </summary>
    private bool _bassMixing = false;
    
    // 低通滤波器状态
    private readonly float[] _bassFilterStates = new float[2];
    
    public override string Name => "Stereo Enhancement";

    
    protected override void OnInitialized()
    {
        base.OnInitialized();
        lock (_lock)
        {
            if (Source.WaveFormat.Channels != 2)
                throw new InvalidOperationException("只支持立体声音频");
        }
    }

    /// <summary>
    /// 读取音频数据并应用立体声增强效果
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
            int channels = Source.WaveFormat.Channels;
            float enhancementFactor = _enhancementFactor;
            float stereoWidth = _stereoWidth;
            bool bassMixing = _bassMixing;
            float highFrequencyBoost = _highFrequencyBoost;
            float dynamicRangeCompression = _dynamicRangeCompression;

            for (int n = 0; n < samplesRead; n += channels)
            {
                // 获取左右声道样本
                int leftIndex = offset + n;
                int rightIndex = leftIndex + 1;
                float left = buffer[leftIndex];
                float right = buffer[rightIndex];

                // 计算中间信号和侧边信号
                float mid = (left + right) * 0.5f;
                float side = (left - right) * enhancementFactor;

                // 应用立体声宽度调整
                side *= stereoWidth;

                // 更新左右声道样本
                buffer[leftIndex] = mid + side;
                buffer[rightIndex] = mid - side;

                // 混合低频信号
                if (bassMixing)
                {
                    // 提取低频信号
                    float bassLeft = ApplyLowPassFilter(left, 0, 200); // 截止频率为 200Hz
                    float bassRight = ApplyLowPassFilter(right, 1, 200);

                    // 混合低频信号
                    buffer[leftIndex] = (buffer[leftIndex] + bassLeft) * 0.5f;
                    buffer[rightIndex] = (buffer[rightIndex] + bassRight) * 0.5f;
                }

                // 高频增强
                buffer[leftIndex] *= highFrequencyBoost;
                buffer[rightIndex] *= highFrequencyBoost;
                
                // 动态范围压缩
                float maxAmplitude = Math.Max(Math.Abs(buffer[leftIndex]), Math.Abs(buffer[rightIndex]));
                if (maxAmplitude > 1.0f)
                {
                    float compressionFactor = 1.0f / (1.0f + dynamicRangeCompression * maxAmplitude);
                    buffer[leftIndex] *= compressionFactor;
                    buffer[rightIndex] *= compressionFactor;
                }
                
                // 软限幅
                buffer[leftIndex] = SoftClip(buffer[leftIndex]);
                buffer[rightIndex] = SoftClip(buffer[rightIndex]);
            }
        }

        return samplesRead;
    }
    
    /// <summary>
    /// 应用低通滤波器提取低频信号
    /// </summary>
    private float ApplyLowPassFilter(float input, int channelIndex, float cutoffFrequency)
    {
        float rc = 1.0f / (2 * MathF.PI * cutoffFrequency);
        float dt = 1.0f / Source.WaveFormat.SampleRate;
        float a = dt / (rc + dt);

        _bassFilterStates[channelIndex] = a * input + (1 - a) * _bassFilterStates[channelIndex];
        return _bassFilterStates[channelIndex];
    }
    
    /// <summary>
    /// 软限幅函数
    /// </summary>
    private static float SoftClip(float sample, float threshold = 0.9f)
    {
        if (Math.Abs(sample) <= threshold)
            return sample;
        return Math.Sign(sample) * (threshold + (1 - threshold) * MathF.Tanh((Math.Abs(sample) - threshold) / (1 - threshold)));
    }

    /// <summary>
    /// 克隆当前效果器的配置
    /// </summary>
    public override IAudioEffect Clone()
    {
        var clone = new StereoEnhancementEffect();
        clone.SetParameter("EnhancementFactor", _enhancementFactor);
        clone.SetParameter("StereoWidth", _stereoWidth);
        clone.SetParameter("BassMixing", _bassMixing);
        clone.SetParameter("HighFrequencyBoost", _highFrequencyBoost);
        clone.SetParameter("DynamicRangeCompression", _dynamicRangeCompression);
        clone.Enabled = Enabled;
        clone.Priority = Priority;
        return clone;
    }

    /// <summary>
    /// 返回当前效果器的调试信息
    /// </summary>
    public override string DebugInfo
    {
        get
        {
            lock (_lock)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"Name: {Name}");
                sb.AppendLine($"Enabled: {Enabled}");
                sb.AppendLine($"Priority: {Priority}");
                sb.AppendLine($"Enhancement Factor: {_enhancementFactor:F2}");
                sb.AppendLine($"Stereo Width: {_stereoWidth:F2}");
                sb.AppendLine($"Bass Mixing: {_bassMixing}");
                sb.AppendLine($"High Frequency Boost: {_highFrequencyBoost:F2}");
                sb.AppendLine($"Dynamic Range Compression: {_dynamicRangeCompression:F2}");
                sb.AppendLine($"Channels: {Source.WaveFormat.Channels}");
                return sb.ToString();
            }
        }
    }

    /// <summary>
    /// 设置效果器的配置参数
    /// </summary>
    public override void SetParameter<T>(string key, T value)
    {
        base.SetParameter(key, value);

        switch (key.ToLower())
        {
            case "enhancementfactor":
                if (value is float factor)
                {
                    lock (_lock)
                    {
                        _enhancementFactor = Math.Clamp(factor, 0.5f, 3.0f);
                    }
                }
                break;

            case "stereowidth":
                if (value is float width)
                {
                    lock (_lock)
                    {
                        _stereoWidth = Math.Clamp(width, 0.0f, 2.0f);
                    }
                }
                break;

            case "bassmixing":
                if (value is bool mixing)
                {
                    lock (_lock)
                    {
                        _bassMixing = mixing;
                    }
                }
                break;

            case "highfrequencyboost":
                if (value is float boost)
                {
                    lock (_lock)
                    {
                        _highFrequencyBoost = Math.Clamp(boost, 0.0f, 2.0f);
                    }
                }
                break;

            case "dynamicrangecompression":
                if (value is float compression)
                {
                    lock (_lock)
                    {
                        _dynamicRangeCompression = Math.Clamp(compression, 0.0f, 1.0f);
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
        return key.ToLower() switch
        {
            "enhancementfactor" => (T)(object)_enhancementFactor,
            "stereowidth" => (T)(object)_stereoWidth,
            "bassmixing" => (T)(object)_bassMixing,
            "highfrequencyboost" => (T)(object)_highFrequencyBoost,
            "dynamicrangecompression" => (T)(object)_dynamicRangeCompression,
            _ => base.GetParameter<T>(key),
        };
    }
}