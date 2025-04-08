using SoundFlow.Abstracts;
using SoundFlow.Enums;
using SoundFlow.Interfaces;
using SoundFlow.Utils;

namespace SoundFlow.Providers;

/// <summary>
///     Provides audio data from a stream.
/// </summary>
public sealed class StreamDataProvider : ISoundDataProvider, IDisposable
{
    private readonly ISoundDecoder _decoder;
    private readonly Stream _stream;

    /// <summary>
    ///     Initializes a new instance of the <see cref="StreamDataProvider" /> class.
    /// </summary>
    /// <param name="stream">The stream to read audio data from.</param>
    /// <param name="sampleRate">The sample rate of the audio data.</param>
    public StreamDataProvider(Stream stream, int? sampleRate = null)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _decoder = AudioEngine.Instance.CreateDecoder(stream);
        SampleRate = sampleRate;

        _decoder.EndOfStreamReached += (_, args) => EndOfStreamReached?.Invoke(this, args);
    }

    /// <inheritdoc />
    public int Position { get; private set; }

    /// <inheritdoc />
    public int Length => _decoder.Length;

    /// <inheritdoc />
    public bool CanSeek => _stream.CanSeek;

    /// <inheritdoc />
    public SampleFormat SampleFormat => _decoder.SampleFormat;

    /// <inheritdoc />
    public int? SampleRate { get; set; }

    /// <inheritdoc />
    public event EventHandler<EventArgs>? EndOfStreamReached;

    /// <inheritdoc />
    public event EventHandler<PositionChangedEventArgs>? PositionChanged;

    /// <inheritdoc />
    public int ReadBytes(Span<float> buffer)
    {
        int count = _decoder.Decode(buffer);
        Position += count;
        return count;
    }

    /// <inheritdoc />
    public void Seek(int sampleOffset)
    {
        if (!CanSeek)
            throw new InvalidOperationException("Seeking is not supported for this stream.");

        if (sampleOffset < 0 || sampleOffset >= Length)
            throw new ArgumentOutOfRangeException(nameof(sampleOffset), "Seek position is outside the valid range.");

        _decoder.Seek(sampleOffset);
        Position = (int)_stream.Position * SampleFormat.GetBytesPerSample();

        PositionChanged?.Invoke(this, new PositionChangedEventArgs(Position));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _decoder.Dispose();
        _stream.Dispose();
    }
}
