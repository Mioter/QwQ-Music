using System;
using SoundFlow.Abstracts;

namespace SoundFlow.Modifiers;

/// <summary>
/// 颤音效果器<br />
/// Tremolo effect modifier
/// </summary>
public sealed class TremoloModifier : SoundModifier
{
    // 状态变量
    private float _phase;
    
    // 配置参数
    private float _rate = 5.0f;
    private float _depth = 0.5f;

    /// <inheritdoc />
    public override string Name { get; set; } = "Tremolo Effect";
    
    /// <summary>
    /// 颤音波形类型<br />
    /// Tremolo waveform type
    /// </summary>
    public enum TremoloWaveform
    {
        /// <summary>正弦波</summary>
        Sine,
        /// <summary>三角波</summary>
        Triangle,
        /// <summary>方波</summary>
        Square,
        /// <summary>锯齿波</summary>
        Sawtooth,
    }
    
    /// <summary>
    /// 颤音速率 (0.1-20.0 Hz)<br />
    /// Tremolo rate (0.1-20.0 Hz)
    /// </summary>
    public float Rate
    {
        get => _rate;
        set => _rate = Math.Clamp(value, 0.1f, 20.0f);
    }
    
    /// <summary>
    /// 颤音深度 (0.0-1.0)<br />
    /// Tremolo depth (0.0-1.0)
    /// </summary>
    public float Depth
    {
        get => _depth;
        set => _depth = Math.Clamp(value, 0.0f, 1.0f);
    }
    
    /// <summary>
    /// 颤音波形<br />
    /// Tremolo waveform
    /// </summary>
    public TremoloWaveform Waveform { get; set; } = TremoloWaveform.Sine;

    /// <inheritdoc />
    public override void Process(Span<float> buffer)
    {
        int channels = AudioEngine.Channels;
        float sampleRate = AudioEngine.Instance.SampleRate;
        
        // 计算相位增量
        float phaseIncrement = _rate / sampleRate;
        
        for (int i = 0; i < buffer.Length; i++)
        {
            // 只在每个声道的第一个样本更新相位
            if (i % channels == 0)
            {
                _phase += phaseIncrement;
                if (_phase >= 1.0f)
                    _phase -= 1.0f;
            }
            
            // 计算调制值
            float modulation = CalculateModulation(_phase);
            
            // 应用颤音效果
            buffer[i] *= modulation;
        }
    }
    
    /// <inheritdoc />
    public override float ProcessSample(float sample, int channel)
    {
        // 在单样本处理中，我们需要更谨慎地更新相位
        // 只在第一个声道更新相位
        if (channel == 0)
        {
            float phaseIncrement = _rate / AudioEngine.Instance.SampleRate;
            _phase += phaseIncrement;
            if (_phase >= 1.0f)
                _phase -= 1.0f;
        }
        
        // 计算调制值
        float modulation = CalculateModulation(_phase);
        
        // 应用颤音效果
        return sample * modulation;
    }
    
    /// <summary>
    /// 根据当前相位和波形类型计算调制值<br />
    /// Calculate modulation value based on current phase and waveform type
    /// </summary>
    private float CalculateModulation(float phase)
    {
        float rawModulation = Waveform switch
        {
            TremoloWaveform.Sine => 0.5f + 0.5f * MathF.Sin(2.0f * MathF.PI * phase),
            TremoloWaveform.Triangle => phase < 0.5f ? 2.0f * phase : 2.0f * (1.0f - phase),
            TremoloWaveform.Square => phase < 0.5f ? 1.0f : 0.0f,
            TremoloWaveform.Sawtooth => phase,
            _ => 0.5f
        };
        
        // 应用深度参数
        return 1.0f - _depth + _depth * rawModulation;
    }
}