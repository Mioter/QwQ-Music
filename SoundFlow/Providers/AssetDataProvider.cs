using SoundFlow.Abstracts;
using SoundFlow.Enums;
using SoundFlow.Interfaces;

namespace SoundFlow.Providers;

/// <summary>
///     Provides audio data from a file or stream.
/// </summary>
/// <remarks>Loads full audio directly to memory.</remarks>
public sealed class AssetDataProvider : ISoundDataProvider, IDisposable
{
    private float[] _data;
    private bool _isDisposed;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AssetDataProvider" /> class.
    /// </summary>
    /// <param name="stream">The stream to read audio data from.</param>
    /// <param name="sampleRate">The sample rate of the audio data.</param>
    public AssetDataProvider(Stream stream, int? sampleRate = null)
    {
        var decoder = AudioEngine.Instance.CreateDecoder(stream);
        _data = Decode(decoder);
        decoder.Dispose();
        SampleRate = sampleRate;
        Length = _data.Length;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="AssetDataProvider" /> class.
    /// </summary>
    /// <param name="data">The audio data to read.</param>
    /// <param name="sampleRate">The sample rate of the audio data.</param>
    public AssetDataProvider(byte[] data, int? sampleRate = null)
        : this(new MemoryStream(data), sampleRate) { }

    /// <inheritdoc />
    public int Position { get; private set; }

    /// <inheritdoc />
    public int Length { get; } // Length in samples

    /// <inheritdoc />
    public bool CanSeek => true;

    /// <inheritdoc />
    public SampleFormat SampleFormat { get; private set; }

    /// <inheritdoc />
    public int? SampleRate { get; set; }

    /// <inheritdoc />
    public event EventHandler<EventArgs>? EndOfStreamReached;

    /// <inheritdoc />
    public event EventHandler<PositionChangedEventArgs>? PositionChanged;

    /// <inheritdoc />
    public int ReadBytes(Span<float> buffer)
    {
        int samplesToRead = Math.Min(buffer.Length, _data.Length - Position);
        _data.AsSpan(Position, samplesToRead).CopyTo(buffer);

        Position += samplesToRead;

        if (Position >= _data.Length)
            EndOfStreamReached?.Invoke(this, EventArgs.Empty);

        PositionChanged?.Invoke(this, new PositionChangedEventArgs(Position));

        return samplesToRead;
    }

    /// <inheritdoc />
    public void Seek(int sampleOffset)
    {
        Position = Math.Clamp(sampleOffset, 0, _data.Length);
        PositionChanged?.Invoke(this, new PositionChangedEventArgs(Position));
    }

    private float[] Decode(ISoundDecoder decoder)
    {
        SampleFormat = decoder.SampleFormat;
        return decoder.Length > 0 ? DecodeKnownLength(decoder) : DecodeUnknownLength(decoder);
    }

    private static float[] DecodeKnownLength(ISoundDecoder decoder)
    {
        float[] samples = new float[decoder.Length];
        int read = decoder.Decode(samples);
        if (read != decoder.Length)
            throw new InvalidOperationException($"Decoding error: Read {read}, expected {decoder.Length} samples.");
        return samples;
    }

    private static float[] DecodeUnknownLength(ISoundDecoder decoder)
    {
        const int blockSize = 22050;
        var blocks = new List<float[]>();
        int samplesRead;
        do
        {
            float[] block = new float[blockSize * AudioEngine.Channels];
            samplesRead = decoder.Decode(block);
            if (samplesRead > 0)
                blocks.Add(block);
        } while (samplesRead == blockSize * AudioEngine.Channels);

        int totalSamples = blocks.Sum(block => block.Length);
        float[] samples = new float[totalSamples];
        int offset = 0;
        foreach (float[]? block in blocks)
        {
            block.CopyTo(samples, offset);
            offset += block.Length;
        }
        return samples;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
            return;

        // Dispose of _data
        _data = null!;
        _isDisposed = true;
    }
}
