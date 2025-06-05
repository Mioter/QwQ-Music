using System.Buffers;
using System.Runtime.InteropServices;
using SoundFlow.Enums;
using SoundFlow.Interfaces;
using SoundFlow.Utils;

namespace SoundFlow.Providers;

/// <summary>
///     Provides audio data from a raw PCM stream.
///     This provider is designed for streams that directly contain raw PCM bytes without any encoding headers.
/// </summary>
public class RawDataProvider : ISoundDataProvider
{
    private readonly Stream _pcmStream;
    private readonly int _channels;
    private readonly int _sampleRate;
    private bool _isDisposed;

    /// <summary>
    ///     Creates a new <see cref="RawDataProvider"/> instance.
    /// </summary>
    /// <param name="pcmStream">The stream containing the raw PCM audio data.</param>
    /// <param name="sampleFormat">The sample format of the PCM data in the stream.</param>
    /// <param name="channels">The number of audio channels in the PCM data.</param>
    /// <param name="sampleRate">The sample rate of the PCM data (samples per second).</param>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="pcmStream"/> cannot be <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    ///     <paramref name="sampleFormat"/> cannot be <see cref="SampleFormat.Unknown"/>.
    /// </exception>
    public RawDataProvider(Stream pcmStream, SampleFormat sampleFormat, int channels, int sampleRate)
    {
        _pcmStream = pcmStream ?? throw new ArgumentNullException(nameof(pcmStream));
        SampleFormat = sampleFormat;
        _channels = channels;
        _sampleRate = sampleRate;

        if (SampleFormat == SampleFormat.Unknown)
            throw new ArgumentException("SampleFormat cannot be Default for RawDataProvider.", nameof(sampleFormat));
    }

    /// <inheritdoc />
    public int Position { get; private set; }

    /// <inheritdoc />
    public int Length
    {
        get
        {
            if (!_pcmStream.CanSeek)
                return -1;
            return (int)(_pcmStream.Length / SampleFormat.GetBytesPerSample() / _channels);
        }
    }

    /// <inheritdoc />
    public bool CanSeek => _pcmStream.CanSeek;

    /// <inheritdoc />
    public SampleFormat SampleFormat { get; }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">
    ///     Always thrown when setting the SampleRate, as it is determined by the constructor for <see cref="RawDataProvider"/>.
    /// </exception>
    public int? SampleRate
    {
        get => _sampleRate;
        set => throw new InvalidOperationException("SampleRate is determined by constructor for RawDataProvider.");
    }

    /// <inheritdoc />
    public event EventHandler<EventArgs>? EndOfStreamReached;

    /// <inheritdoc />
    public event EventHandler<PositionChangedEventArgs>? PositionChanged;

    /// <inheritdoc />
    /// <exception cref="ObjectDisposedException">Thrown if the provider has been disposed.</exception>
    public int ReadBytes(Span<float> buffer)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        int bytesPerSample = SampleFormat.GetBytesPerSample();
        int samplesToRead = buffer.Length;
        int bytesToRead = samplesToRead * bytesPerSample;

        byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(bytesToRead);
        var byteBuffer = rentedBuffer.AsSpan(0, bytesToRead);

        int bytesActuallyRead = _pcmStream.Read(byteBuffer);
        int samplesActuallyRead = bytesActuallyRead / bytesPerSample;

        if (samplesActuallyRead == 0)
        {
            EndOfStreamReached?.Invoke(this, EventArgs.Empty);
            ArrayPool<byte>.Shared.Return(rentedBuffer);
            return 0;
        }

        ConvertBytesToFloat(byteBuffer[..bytesActuallyRead], buffer[..samplesActuallyRead], SampleFormat);

        Position += samplesActuallyRead;
        PositionChanged?.Invoke(this, new PositionChangedEventArgs(Position));

        ArrayPool<byte>.Shared.Return(rentedBuffer);
        return samplesActuallyRead;
    }

    private static void ConvertBytesToFloat(Span<byte> byteBuffer, Span<float> floatBuffer, SampleFormat format)
    {
        // Similar logic to MiniAudioDecoder.ConvertToFloatIfNecessary, but without decoder involved.
        int sampleCount = floatBuffer.Length;

        switch (format)
        {
            case SampleFormat.U8:
                byte[] u8Span = byteBuffer.ToArray();
                for (int i = 0; i < sampleCount; i++)
                {
                    if (i < u8Span.Length)
                        floatBuffer[i] = (u8Span[i] - 128) / 128f;
                    else
                        floatBuffer[i] = 0;
                }
                break;
            case SampleFormat.S16:
                var shortSpan = MemoryMarshal.Cast<byte, short>(byteBuffer);
                for (int i = 0; i < sampleCount; i++)
                    floatBuffer[i] = shortSpan[i] / (float)short.MaxValue;
                break;
            case SampleFormat.S24:
                for (int i = 0; i < sampleCount; i++)
                {
                    int byteIndex = i * 3;
                    if (byteIndex + 2 < byteBuffer.Length)
                    {
                        int sample24 =
                            byteBuffer[byteIndex] << 0
                            | byteBuffer[byteIndex + 1] << 8
                            | byteBuffer[byteIndex + 2] << 16;
                        if ((sample24 & 0x800000) != 0)
                            sample24 |= unchecked((int)0xFF000000);
                        floatBuffer[i] = sample24 / 8388608f;
                    }
                    else
                    {
                        floatBuffer[i] = 0;
                    }
                }
                break;
            case SampleFormat.S32:
                var int32Span = MemoryMarshal.Cast<byte, int>(byteBuffer);
                for (int i = 0; i < sampleCount; i++)
                    floatBuffer[i] = int32Span[i] / (float)int.MaxValue;
                break;
            case SampleFormat.F32:
                var floatSpan = MemoryMarshal.Cast<byte, float>(byteBuffer);
                floatSpan.CopyTo(floatBuffer);
                break;
            case SampleFormat.Unknown:
            default:
                throw new NotSupportedException($"Sample format {format} is not supported for RawDataProvider.");
        }
    }

    /// <inheritdoc />
    /// <exception cref="ObjectDisposedException">Thrown if the provider has been disposed.</exception>
    /// <exception cref="NotSupportedException">Thrown if seeking is not supported on the underlying PCM stream.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="sampleOffset"/> is negative or outside the valid range.</exception>
    public void Seek(int sampleOffset)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (!_pcmStream.CanSeek)
            throw new NotSupportedException("Seeking is not supported for the underlying PCM stream.");

        if (sampleOffset < 0)
            sampleOffset = 0;

        long byteOffset = (long)sampleOffset * SampleFormat.GetBytesPerSample() * _channels;

        if (byteOffset > _pcmStream.Length)
            byteOffset = _pcmStream.Length;

        _pcmStream.Seek(byteOffset, SeekOrigin.Begin);
        Position = sampleOffset;
        PositionChanged?.Invoke(this, new PositionChangedEventArgs(Position));
    }

    /// <summary>
    ///     Releases the unmanaged resources used by the <see cref="RawDataProvider"/> and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">
    ///     <see langword="true"/> to release both managed and unmanaged resources;
    ///     <see langword="false"/> to release only unmanaged resources.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
            return;

        if (disposing)
            _pcmStream.Dispose();

        _isDisposed = true;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
