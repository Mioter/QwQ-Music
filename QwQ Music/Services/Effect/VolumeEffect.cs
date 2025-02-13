using System;
using NAudio.Wave;

namespace QwQ_Music.Services.Effect;

/// <summary>
/// 音量控制
/// </summary>
public class VolumeEffect : AudioEffectBase
{
    private float _volume = 1.0f;

    public VolumeEffect(ISampleProvider source) : base(source)
    {
        ValidateWaveFormat(source.WaveFormat); // 确保音频格式兼容
        if (source.WaveFormat.Channels > 2)
            throw new NotSupportedException("只支持单声道/立体声");
    }

    public override string Name => "音量控制";

    public float Volume
    {
        get => _volume;
        set => _volume = Math.Clamp(value, 0.0f, 2.0f);
    }

    public override int Read(float[] buffer, int offset, int count)
    {
        int samplesRead = Source.Read(buffer, offset, count);
        
        for (int i = 0; i < samplesRead; i++)
        {
            buffer[offset + i] *= _volume;
        }
        return samplesRead;
    }
}
