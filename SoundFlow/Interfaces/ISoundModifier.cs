namespace SoundFlow.Interfaces;

/// <summary>
///     音效修饰器的统一接口<br />
///     Unified interface for sound effects modifiers
/// </summary>
public interface ISoundModifier
{
    /// <summary>
    /// 修饰器名称<br />
    /// Name of the modifier
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// 修饰器启用状态<br />
    /// Enabled state of the modifier
    /// </summary>
    public bool Enabled { get; set; }
    
    /// <summary>
    /// 对音频样本缓冲区应用修饰效果<br />
    /// Applies the modifier to a buffer of audio samples
    /// </summary>
    /// <param name="buffer">包含要修改的音频样本的缓冲区<br />The buffer containing the audio samples to modify</param>
    public void Process(Span<float> buffer);
    
    /// <summary>
    /// 处理单个音频样本<br />
    /// Processes a single audio sample
    /// </summary>
    /// <param name="sample">输入的音频样本<br />The input audio sample</param>
    /// <param name="channel">样本所属的声道<br />The channel the sample belongs to</param>
    /// <returns>修改后的音频样本<br />The modified audio sample</returns>
    public float ProcessSample(float sample, int channel);
}
