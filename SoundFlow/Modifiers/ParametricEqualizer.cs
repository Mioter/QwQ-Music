using SoundFlow.Abstracts;

namespace SoundFlow.Modifiers;

/// <summary>
/// 参数化均衡器，支持多种滤波器类型<br />
/// A Parametric Equalizer with support for multiple filter types.
/// </summary>
public sealed class ParametricEqualizer : SoundModifier
{
    /// <inheritdoc />
    public override string Name { get; set; } = "Parametric Equalizer";

    /// <summary>
    /// 均衡器应用的频段列表<br />
    /// List of EQ bands applied by this equalizer.
    /// </summary>
    public List<EqualizerBand> Bands { get; set; } = [];

    private readonly Dictionary<int, List<BiquadFilter>> _filtersPerChannel = [];

    /// <summary>
    /// 构造函数，初始化默认的9段均衡器（3低频、3中频、3高频）<br />
    /// Constructor, initializes the default 9-band equalizer (3 low, 3 mid, 3 high)
    /// </summary>
    public ParametricEqualizer()
    {
        InitializeDefaultBands();
        InitializeFilters();
    }

    /// <summary>
    /// 初始化默认的9段均衡器频段<br />
    /// Initialize default 9-band equalizer bands
    /// </summary>
    private void InitializeDefaultBands()
    {
        // 创建默认频段
        Bands =
        [
            new PeakingBand { Frequency = 60, Q = 1.0f }, // 低频增强 (Sub-bass)
            new PeakingBand { Frequency = 150, Q = 1.0f }, // 低频 (Bass)
            new PeakingBand { Frequency = 250, Q = 1.0f }, // 低中频 (Low-mids)
            // 中频段 (Mid frequencies)
            new PeakingBand { Frequency = 500, Q = 1.0f }, // 中频 (Mids)
            new PeakingBand { Frequency = 1000, Q = 1.0f }, // 中频 (Mids)
            new PeakingBand { Frequency = 2000, Q = 1.0f }, // 高中频 (Upper-mids)
            // 高频段 (High frequencies)
            new PeakingBand { Frequency = 4000, Q = 1.0f }, // 高频 (Presence)
            new PeakingBand { Frequency = 8000, Q = 1.0f }, // 高频 (Brilliance)
            new PeakingBand { Frequency = 16000, Q = 1.0f }, // 超高频 (Air)
        ];

        SetBandsOwner();
    }

    /// <summary>
    /// 设置频段所有者
    /// </summary>
    public void SetBandsOwner()
    {
        // 设置所有者并添加到列表
        foreach (var band in Bands.ToList())
        {
            band.SetOwner(this);
        }
    }

    /// <summary>
    /// 更新所有滤波器系数<br />
    /// Update all filter coefficients
    /// </summary>
    public void UpdateFilters() => InitializeFilters();

    /// <summary>
    /// 根据当前均衡器频段为每个声道初始化滤波器<br />
    /// Initializes the filters for each channel based on the current EQ bands.
    /// </summary>
    private void InitializeFilters()
    {
        _filtersPerChannel.Clear();
        for (int channel = 0; channel < AudioEngine.Channels; channel++)
        {
            var filters = Bands
                .Select(band =>
                {
                    var filter = new BiquadFilter();
                    filter.UpdateCoefficients(band, AudioEngine.Instance.SampleRate);
                    return filter;
                })
                .ToList();

            _filtersPerChannel[channel] = filters;
        }
    }

    /// <inheritdoc/>
    public override void Process(Span<float> buffer)
    {
        for (int i = 0; i < buffer.Length; i++)
        {
            int channel = i % AudioEngine.Channels;
            buffer[i] = ProcessSample(buffer[i], channel);
        }
    }

    /// <inheritdoc/>
    public override float ProcessSample(float sample, int channel)
    {
        if (_filtersPerChannel.TryGetValue(channel, out var value))
            return value.Aggregate(sample, (current, filter) => filter.ProcessSample(current));

        // 如果尚未为此通道初始化滤波器，则进行初始化
        var filters = Bands
            .Select(band =>
            {
                var filter = new BiquadFilter();
                filter.UpdateCoefficients(band, AudioEngine.Instance.SampleRate);
                return filter;
            })
            .ToList();

        _filtersPerChannel[channel] = filters;
        return filters.Aggregate(sample, (current, filter) => filter.ProcessSample(current));
    }

    /// <summary>
    /// 添加一个均衡器频段并重新初始化滤波器<br />
    /// Adds an EQ band to the equalizer and reinitializes the filters.
    /// </summary>
    public void AddBand(EqualizerBand band)
    {
        band.SetOwner(this);
        Bands.Add(band);
        InitializeFilters();
    }

    /// <summary>
    /// 添加多个均衡器频段并重新初始化滤波器<br />
    /// Adds multiple EQ bands to the equalizer and reinitializes the filters.
    /// </summary>
    public void AddBands(IEnumerable<EqualizerBand> bands)
    {
        var equalizerBands = bands.ToList();
        foreach (var band in equalizerBands)
            band.SetOwner(this);

        Bands.AddRange(equalizerBands);
        InitializeFilters();
    }

    /// <summary>
    /// 从均衡器中移除一个频段并重新初始化滤波器<br />
    /// Removes an EQ band from the equalizer and reinitializes the filters.
    /// </summary>
    public void RemoveBand(EqualizerBand band)
    {
        Bands.Remove(band);
        InitializeFilters();
    }

    /// <summary>
    /// 清除所有均衡器频段<br />
    /// Clears all equalizer bands
    /// </summary>
    public void ClearBands()
    {
        Bands.Clear();
        InitializeFilters();
    }

    /// <summary>
    /// 重置所有频段增益为0dB<br />
    /// Reset all band gains to 0dB
    /// </summary>
    public void ResetBands()
    {
        foreach (var band in Bands)
            band.GainDb = 0;

        InitializeFilters();
    }
}

/// <summary>
/// 参数化均衡器支持的滤波器类型<br />
/// Types of filters supported by the Parametric Equalizer.
/// </summary>
public enum FilterType
{
    /// <summary>
    /// 峰值均衡器，提升或削减特定频率范围<br />
    /// A peaking equalizer boosts or cuts a specific frequency range.
    /// </summary>
    Peaking,

    /// <summary>
    /// 低架式均衡器，提升或削减特定频率以下的所有频率<br />
    /// A low-shelf equalizer boosts or cuts all frequencies below a specific frequency.
    /// </summary>
    LowShelf,

    /// <summary>
    /// 高架式均衡器，提升或削减特定频率以上的所有频率<br />
    /// A high-shelf equalizer boosts or cuts all frequencies above a specific frequency.
    /// </summary>
    HighShelf,

    /// <summary>
    /// 低通滤波器，移除音频信号中的高频成分<br />
    /// A low-pass filter removes high frequencies from the audio signal.
    /// </summary>
    LowPass,

    /// <summary>
    /// 高通滤波器，移除音频信号中的低频成分<br />
    /// A high-pass filter removes low frequencies from the audio signal.
    /// </summary>
    HighPass,

    /// <summary>
    /// 带通滤波器，移除特定频率范围外的所有频率<br />
    /// A band-pass filter removes all frequencies outside a specific frequency range.
    /// </summary>
    BandPass,

    /// <summary>
    /// 陷波滤波器，移除音频信号中的特定频率范围<br />
    /// A notch filter removes a specific frequency range from the audio signal.
    /// </summary>
    Notch,

    /// <summary>
    /// 全通滤波器，改变音频信号的相位而不影响其频率响应<br />
    /// An all-pass filter changes the phase of the audio signal without affecting its frequency response.
    /// </summary>
    AllPass,
}

/// <summary>
/// 表示具有特定参数的均衡器频段<br />
/// Represents an EQ band with specific parameters.
/// </summary>
public class EqualizerBand
{
    private float _frequency;
    private float _gainDb;
    private float _q = 0.7071f;
    private float _s = 1f;
    private FilterType _type;

    // 添加对参数化均衡器的引用
    private ParametricEqualizer? _owner;

    /// <summary>
    /// 设置此频段所属的均衡器<br />
    /// Set the equalizer that owns this band
    /// </summary>
    internal void SetOwner(ParametricEqualizer owner)
    {
        _owner = owner;
    }

    /// <summary>
    /// 均衡器频段的中心频率（赫兹）<br />
    /// The center frequency of the EQ band in Hz.
    /// </summary>
    public float Frequency
    {
        get => _frequency;
        set
        {
            _frequency = value;
            _owner?.UpdateFilters();
        }
    }

    /// <summary>
    /// 均衡器频段的增益（分贝）<br />
    /// The gain of the EQ band in decibels.
    /// </summary>
    public float GainDb
    {
        get => _gainDb;
        set
        {
            _gainDb = value;
            _owner?.UpdateFilters();
        }
    }

    /// <summary>
    /// 均衡器频段的品质因数<br />
    /// The quality factor of the EQ band.
    /// </summary>
    public float Q
    {
        get => _q;
        set
        {
            _q = value;
            _owner?.UpdateFilters();
        }
    }

    /// <summary>
    /// 均衡器频段的增益乘数<br />
    /// The gain multiplier of the EQ band.
    /// </summary>
    public float S
    {
        get => _s;
        set
        {
            _s = value;
            _owner?.UpdateFilters();
        }
    }

    /// <summary>
    /// 要应用的滤波器类型<br />
    /// The type of filter to apply.
    /// </summary>
    public FilterType Type
    {
        get => _type;
        set
        {
            _type = value;
            _owner?.UpdateFilters();
        }
    }
}

/// <summary>
/// 峰值均衡器频段<br />
/// Peaking equalizer band
/// </summary>
public class PeakingBand : EqualizerBand
{
    /// <inheritdoc />
    public PeakingBand()
    {
        Type = FilterType.Peaking;
    }
}

/// <summary>
/// 低架式均衡器频段<br />
/// Low shelf equalizer band
/// </summary>
public class LowShelfBand : EqualizerBand
{
    /// <inheritdoc />
    public LowShelfBand()
    {
        Type = FilterType.LowShelf;
        Q = 0.7071f;
    }
}

/// <summary>
/// 高架式均衡器频段<br />
/// High shelf equalizer band
/// </summary>
public class HighShelfBand : EqualizerBand
{
    /// <inheritdoc />
    public HighShelfBand()
    {
        Type = FilterType.HighShelf;
        Q = 0.7071f;
    }
}

/// <summary>
/// 低通滤波器频段<br />
/// Low pass filter band
/// </summary>
public class LowPassBand : EqualizerBand
{
    /// <inheritdoc />
    public LowPassBand()
    {
        Type = FilterType.LowPass;
        Q = 0.7071f;
    }
}

/// <summary>
/// 高通滤波器频段<br />
/// High pass filter band
/// </summary>
public class HighPassBand : EqualizerBand
{
    /// <inheritdoc />
    public HighPassBand()
    {
        Type = FilterType.HighPass;
        Q = 0.7071f;
    }
}

/// <summary>
/// 带通滤波器频段<br />
/// Band pass filter band
/// </summary>
public class BandPassBand : EqualizerBand
{
    /// <inheritdoc />
    public BandPassBand()
    {
        Type = FilterType.BandPass;
    }
}

/// <summary>
/// 陷波滤波器频段<br />
/// Notch filter band
/// </summary>
public class NotchBand : EqualizerBand
{
    /// <inheritdoc />
    public NotchBand()
    {
        Type = FilterType.Notch;
    }
}

/// <summary>
/// 全通滤波器频段<br />
/// All pass filter band
/// </summary>
public class AllPassBand : EqualizerBand
{
    /// <inheritdoc />
    public AllPassBand()
    {
        Type = FilterType.AllPass;
    }
}

/// <summary>
/// 用于处理音频样本的双二阶滤波器<br />
/// A biquad filter used to process audio samples.
/// </summary>
public class BiquadFilter
{
    private float _a0,
        _a1,
        _a2,
        _b0,
        _b1,
        _b2;
    private float _x1,
        _x2,
        _y1,
        _y2;

    /// <summary>
    /// 根据指定的均衡器频段参数更新滤波器系数<br />
    /// Updates the filter coefficients based on the specified EQ band parameters.
    /// </summary>
    /// <param name="band"/>包含滤波器参数的均衡器频段<br />The EQ
    /// <param name="sampleRate"/>音频数据的采样率<br />The sample rate of the audio data.
    public void UpdateCoefficients(EqualizerBand band, float sampleRate)
    {
        float a;
        float omega = 2 * (float)Math.PI * band.Frequency / sampleRate;
        float sinOmega = (float)Math.Sin(omega);
        float cosOmega = (float)Math.Cos(omega);
        float alpha;

        switch (band.Type)
        {
            case FilterType.Peaking:
                a = (float)Math.Pow(10, band.GainDb / 40);
                alpha = sinOmega / (2 * band.Q);

                _b0 = 1 + alpha * a;
                _b1 = -2 * cosOmega;
                _b2 = 1 - alpha * a;
                _a0 = 1 + alpha / a;
                _a1 = -2 * cosOmega;
                _a2 = 1 - alpha / a;
                break;
            case FilterType.LowShelf:
                a = (float)Math.Pow(10, band.GainDb / 40);
                float sqrtA = (float)Math.Sqrt(a);
                alpha = sinOmega / 2 * (float)Math.Sqrt((a + 1 / a) * (1 / band.S - 1) + 2);

                _b0 = a * (a + 1 - (a - 1) * cosOmega + 2 * sqrtA * alpha);
                _b1 = 2 * a * (a - 1 - (a + 1) * cosOmega);
                _b2 = a * (a + 1 - (a - 1) * cosOmega - 2 * sqrtA * alpha);
                _a0 = a + 1 + (a - 1) * cosOmega + 2 * sqrtA * alpha;
                _a1 = -2 * (a - 1 + (a + 1) * cosOmega);
                _a2 = a + 1 + (a - 1) * cosOmega - 2 * sqrtA * alpha;
                break;
            case FilterType.HighShelf:
                a = (float)Math.Pow(10, band.GainDb / 40);
                sqrtA = (float)Math.Sqrt(a);
                alpha = sinOmega / 2 * (float)Math.Sqrt((a + 1 / a) * (1 / band.S - 1) + 2);

                _b0 = a * (a + 1 + (a - 1) * cosOmega + 2 * sqrtA * alpha);
                _b1 = -2 * a * (a - 1 + (a + 1) * cosOmega);
                _b2 = a * (a + 1 + (a - 1) * cosOmega - 2 * sqrtA * alpha);
                _a0 = a + 1 - (a - 1) * cosOmega + 2 * sqrtA * alpha;
                _a1 = 2 * (a - 1 - (a + 1) * cosOmega);
                _a2 = a + 1 - (a - 1) * cosOmega - 2 * sqrtA * alpha;
                break;
            case FilterType.LowPass:
                alpha = sinOmega / (2 * band.Q);

                _b0 = (1 - cosOmega) / 2;
                _b1 = 1 - cosOmega;
                _b2 = (1 - cosOmega) / 2;
                _a0 = 1 + alpha;
                _a1 = -2 * cosOmega;
                _a2 = 1 - alpha;
                break;
            case FilterType.HighPass:
                alpha = sinOmega / (2 * band.Q);

                _b0 = (1 + cosOmega) / 2;
                _b1 = -(1 + cosOmega);
                _b2 = (1 + cosOmega) / 2;
                _a0 = 1 + alpha;
                _a1 = -2 * cosOmega;
                _a2 = 1 - alpha;
                break;
            case FilterType.BandPass:
                alpha = sinOmega / (2 * band.Q);

                _b0 = alpha;
                _b1 = 0;
                _b2 = -alpha;
                _a0 = 1 + alpha;
                _a1 = -2 * cosOmega;
                _a2 = 1 - alpha;
                break;
            case FilterType.Notch:
                alpha = sinOmega / (2 * band.Q);

                _b0 = 1;
                _b1 = -2 * cosOmega;
                _b2 = 1;
                _a0 = 1 + alpha;
                _a1 = -2 * cosOmega;
                _a2 = 1 - alpha;
                break;
            case FilterType.AllPass:
                alpha = sinOmega / (2 * band.Q);

                _b0 = 1 - alpha;
                _b1 = -2 * cosOmega;
                _b2 = 1 + alpha;
                _a0 = 1 + alpha;
                _a1 = -2 * cosOmega;
                _a2 = 1 - alpha;
                break;
            default:
                throw new NotImplementedException("Filter type not implemented");
        }

        // Normalize the coefficients
        _b0 /= _a0;
        _b1 /= _a0;
        _b2 /= _a0;
        _a1 /= _a0;
        _a2 /= _a0;
    }

    /// <summary>
    /// 通过双二阶滤波器处理单个音频样本<br />
    /// Processes a single audio sample through the biquad filter.
    /// </summary>
    /// <param name="x">输入样本<br />The input sample.</param>
    /// <returns>滤波后的输出样本<br />The filtered output sample.</returns>
    public float ProcessSample(float x)
    {
        float y = _b0 * x + _b1 * _x1 + _b2 * _x2 - _a1 * _y1 - _a2 * _y2;

        // 移动数据
        // Shift the data
        _x2 = _x1;
        _x1 = x;
        _y2 = _y1;
        _y1 = y;

        return y;
    }
}
