using System;
using System.Buffers;
using System.IO;
using SoundFlow.Abstracts;
using SoundFlow.Backends.MiniAudio.Enums;
using SoundFlow.Enums;
using SoundFlow.Exceptions;
using SoundFlow.Interfaces;

namespace SoundFlow.Backends.MiniAudio;

/// <summary>
///     An object to assist with converting audio formats into raw PCM frames.
/// </summary>
internal sealed unsafe class MiniAudioDecoder : ISoundDecoder
{
    private readonly nint _decoder;
    private readonly Stream _stream;
    private readonly Native.DecoderRead _readCallback;
    private readonly Native.DecoderSeek _seekCallback;
    private bool _endOfStreamReached;
    private byte[] _readBuffer = [];
    private short[]? _shortBuffer;
    private int[]? _intBuffer;
    private byte[]? _byteBuffer;
    private readonly object _syncLock = new();

    /// <summary>
    ///     Constructs a new decoder from the given stream in one of the supported formats.
    /// </summary>
    /// <param name="stream">A stream to a file or streaming audio source in one of the supported formats.</param>
    public MiniAudioDecoder(Stream stream)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        SampleFormat = AudioEngine.Instance.SampleFormat;

        IntPtr configPtr = Native.AllocateDecoderConfig(AudioEngine.Instance.SampleFormat, (uint)AudioEngine.Channels,
            (uint)AudioEngine.Instance.SampleRate);

        _decoder = Native.AllocateDecoder();
        var result = Native.DecoderInit(_readCallback = ReadCallback, _seekCallback = SeekCallback, nint.Zero,
            configPtr, _decoder);

        if (result != Result.Success) throw new BackendException("MiniAudio", result, "Unable to initialize decoder.");

        result = Native.DecoderGetLengthInPcmFrames(_decoder, out uint* length);
        if (result != Result.Success) throw new BackendException("MiniAudio", result, "Unable to get decoder length.");
        Length = (int)length * AudioEngine.Channels;
        _endOfStreamReached = false;
    }

    /// <inheritdoc />
    public bool IsDisposed { get; private set; }

    /// <inheritdoc />
    public int Length { get; private set; }

    /// <inheritdoc />
    public SampleFormat SampleFormat { get; }

    public event EventHandler<EventArgs>? EndOfStreamReached;

    /// <summary>
    ///     Decodes the next several samples.
    /// </summary>
    public int Decode(Span<float> samples)
    {
        lock (_syncLock)
        {
            if (IsDisposed || _endOfStreamReached)
                return 0;

            uint framesToRead = (uint)(samples.Length / AudioEngine.Channels);
            IntPtr nativeBuffer = GetNativeBufferPointer(samples);

            if (_endOfStreamReached ||
                framesToRead == 0 ||
                Native.DecoderReadPcmFrames(_decoder, nativeBuffer, framesToRead, out uint* framesRead) != Result.Success ||
                (uint)framesRead == 0)
            {
                _endOfStreamReached = true;
                EndOfStreamReached?.Invoke(this, EventArgs.Empty);
                return 0;
            }

            if (SampleFormat != SampleFormat.F32)
                ConvertToFloatIfNecessary(samples, (uint)framesRead, nativeBuffer);

            return (int)framesRead * AudioEngine.Channels;
        }
    }

    private nint GetNativeBufferPointer(Span<float> samples)
    {
        switch (SampleFormat)
        {
            case SampleFormat.S16:
                _shortBuffer = ArrayPool<short>.Shared.Rent(samples.Length);
                fixed (short* pSamples = _shortBuffer)
                    return (nint)pSamples;
            case SampleFormat.S24:
                _byteBuffer = ArrayPool<byte>.Shared.Rent(samples.Length * 3);
                fixed (byte* pSamples = _byteBuffer)
                    return (nint)pSamples;
            case SampleFormat.S32:
                _intBuffer = ArrayPool<int>.Shared.Rent(samples.Length);
                fixed (int* pSamples = _intBuffer)
                    return (nint)pSamples;
            case SampleFormat.U8:
                _byteBuffer = ArrayPool<byte>.Shared.Rent(samples.Length);
                fixed (byte* pSamples = _byteBuffer)
                    return (nint)pSamples;
            case SampleFormat.F32:
                fixed (float* pSamples = samples)
                    return (nint)pSamples;
            default:
                throw new NotSupportedException($"Sample format {SampleFormat} is not supported.");
        }
    }

    private void ConvertToFloatIfNecessary(Span<float> samples, uint framesRead, nint nativeBuffer)
    {
        int sampleCount = (int)framesRead * AudioEngine.Channels;
        switch (SampleFormat)
        {
            case SampleFormat.S16:
                var shortSpan = new Span<short>(nativeBuffer.ToPointer(), sampleCount);
                for (int i = 0; i < sampleCount; i++)
                    samples[i] = shortSpan[i] / (float)short.MaxValue;
                if (_shortBuffer != null) ArrayPool<short>.Shared.Return(_shortBuffer);
                _shortBuffer = null!;
                break;
            case SampleFormat.S24:
                var s24Bytes = new Span<byte>(nativeBuffer.ToPointer(), sampleCount * 3); // 3 bytes per sample
                for (int i = 0; i < sampleCount; i++)
                {
                    int sample24 = s24Bytes[i * 3] << 0 | s24Bytes[i * 3 + 1] << 8 | s24Bytes[i * 3 + 2] << 16;
                    if ((sample24 & 0x800000) != 0) // Sign extension for negative values
                        sample24 |= unchecked((int)0xFF000000);
                    samples[i] = sample24 / 8388608f;
                }

                if (_byteBuffer != null) ArrayPool<byte>.Shared.Return(_byteBuffer);
                _byteBuffer = null;
                break;
            case SampleFormat.S32:
                var int32Span = new Span<int>(nativeBuffer.ToPointer(), sampleCount);
                for (int i = 0; i < sampleCount; i++)
                    samples[i] = int32Span[i] / (float)int.MaxValue;
                if (_intBuffer != null) ArrayPool<int>.Shared.Return(_intBuffer);
                _intBuffer = null!;
                break;
            case SampleFormat.U8:
                var byteSpan = new Span<byte>(nativeBuffer.ToPointer(), sampleCount);
                for (int i = 0; i < sampleCount; i++)
                    samples[i] = (byteSpan[i] - 128) / 128f; // Scale U8 to -1.0 to 1.0
                if (_byteBuffer != null) ArrayPool<byte>.Shared.Return(_byteBuffer);
                _byteBuffer = null!;
                break;
        }
    }

    /// <summary>
    ///     Seek to start decoding at the given offset.
    /// </summary>
    public bool Seek(int offset)
    {
        lock (_syncLock)
        {
            Result result;
            if (Length == 0)
            {
                result = Native.DecoderGetLengthInPcmFrames(_decoder, out uint* length);
                if (result != Result.Success || (int)length == 0) return false;
                Length = (int)length * AudioEngine.Channels;
            }

            _endOfStreamReached = false;
            result = Native.DecoderSeekToPcmFrame(_decoder, (ulong)(offset / AudioEngine.Channels));
            return result == Result.Success;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~MiniAudioDecoder()
    {
        Dispose(false);
    }

    private Result ReadCallback(nint pDecoder, nint pBufferOut, ulong bytesToRead, out uint* pBytesRead)
    {
        lock (_syncLock)
        {
            if (!_stream.CanRead || _endOfStreamReached)
            {
                pBytesRead = (uint*)0;
                return Result.NoDataAvailable;
            }

            // Read the next chunk of bytes
            int size = (int)bytesToRead;
            if (_readBuffer.Length < size)
                Array.Resize(ref _readBuffer, size);

            int read = _stream.Read(_readBuffer, 0, size);
            // Check for end of stream
            if (read == 0 && !_endOfStreamReached)
            {
                _endOfStreamReached = true;
                EndOfStreamReached?.Invoke(this, EventArgs.Empty);
            }

            // Copy from read buffer to write buffer
            fixed (byte* pReadBuffer = _readBuffer)
            {
                Buffer.MemoryCopy(pReadBuffer, (void*)pBufferOut, size, read);
            }

            // Clear read buffer
            Array.Clear(_readBuffer, 0, _readBuffer.Length);

            pBytesRead = (uint*)read;
            return Result.Success;
        }
    }

    private Result SeekCallback(nint _, long byteOffset, SeekPoint point)
    {
        lock (_syncLock)
        {
            if (!_stream.CanSeek)
                return Result.NoDataAvailable;

            if (byteOffset >= 0 && byteOffset < _stream.Length - 1)
                _stream.Seek(byteOffset, point == SeekPoint.FromCurrent ? SeekOrigin.Current : SeekOrigin.Begin);

            return Result.Success;
        }
    }

    private void Dispose(bool disposeManaged)
    {
        lock (_syncLock)
        {
            if (IsDisposed) return;
            if (disposeManaged)
            {
                if (_shortBuffer != null)
                {
                    ArrayPool<short>.Shared.Return(_shortBuffer);
                    _shortBuffer = null;
                }

                if (_intBuffer != null)
                {
                    ArrayPool<int>.Shared.Return(_intBuffer);
                    _intBuffer = null;
                }

                if (_byteBuffer != null)
                {
                    ArrayPool<byte>.Shared.Return(_byteBuffer);
                    _byteBuffer = null;
                }
            }

            // keep delegates alive
            GC.KeepAlive(_readCallback);
            GC.KeepAlive(_seekCallback);

            Native.DecoderUninit(_decoder);
            Native.Free(_decoder);

            IsDisposed = true;
        }
    }
}