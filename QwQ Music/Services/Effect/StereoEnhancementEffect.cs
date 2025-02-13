using System;
using NAudio.Wave;

namespace QwQ_Music.Services.Effect;

/// <summary>
/// 立体声增强效果器
/// </summary>
public class StereoEnhancementEffect : AudioEffectBase
{
    private float _enhancementFactor = 1.5f; // 增强因子

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="source">音频源</param>
    public StereoEnhancementEffect(ISampleProvider source) 
        : base(source)
    {
        ValidateWaveFormat(source.WaveFormat); // 确保音频格式兼容
        ArgumentNullException.ThrowIfNull(source);
        if (source.WaveFormat.Channels != 2)
            throw new InvalidOperationException("只支持立体声音频");
    }

    public override string Name => "立体声增强";

    /// <summary>
    /// 增强因子（范围：0.5 到 3.0）
    /// </summary>
    public float EnhancementFactor
    {
        get => _enhancementFactor;
        set => _enhancementFactor = Math.Clamp(value, 0.5f, 3.0f);
    }

    /// <summary>
    /// 读取音频数据并应用立体声增强效果
    /// </summary>
    /// <param name="buffer">音频缓冲区</param>
    /// <param name="offset">起始偏移量</param>
    /// <param name="count">要读取的样本数</param>
    /// <returns>实际读取的样本数</returns>
    public override int Read(float[] buffer, int offset, int count)
    {
        int samplesRead = Source.Read(buffer, offset, count);

        // 缓存增强因子和声道数
        float enhancementFactor = _enhancementFactor;
        int channels = Source.WaveFormat.Channels;

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

            // 更新左右声道样本
            buffer[leftIndex] = mid + side;
            buffer[rightIndex] = mid - side;
        }

        return samplesRead;
    }
}