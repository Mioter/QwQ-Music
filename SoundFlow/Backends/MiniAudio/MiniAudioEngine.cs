using System;
using System.IO;
using System.Linq;
using SoundFlow.Abstracts;
using SoundFlow.Enums;
using SoundFlow.Interfaces;
using SoundFlow.Structs;
using SoundFlow.Utils;

namespace SoundFlow.Backends.MiniAudio;

/// <summary>
///     An audio engine based on the MiniAudio library.
/// </summary>
public sealed class MiniAudioEngine(
    int sampleRate,
    Capability capability,
    SampleFormat sampleFormat = SampleFormat.F32,
    int channels = 2)
    : AudioEngine(sampleRate, capability, sampleFormat, channels)
{
    private Native.AudioCallback? _audioCallback;
    private nint _context;
    private nint _device = nint.Zero;

    /// <inheritdoc />
    protected override bool RequiresBackendThread { get; } = false;


    /// <inheritdoc />
    protected override void InitializeAudioDevice()
    {
        _context = Native.AllocateContext();
        var result = Native.ContextInit(nint.Zero, 0, nint.Zero, _context);
        if (result != Result.Success)
            throw new InvalidOperationException("Unable to init context. " + result);

        InitializeDeviceInternal(nint.Zero, DeviceType.Playback);
    }


    private void InitializeDeviceInternal(nint deviceId, DeviceType type)
    {
        if (_device != nint.Zero) 
            CleanupCurrentDevice();

        IntPtr deviceConfig = Native.AllocateDeviceConfig(Capability, SampleFormat, (uint)Channels, (uint)SampleRate,
            _audioCallback ??= AudioCallback,
            type == DeviceType.Playback ? deviceId : nint.Zero,
            type == DeviceType.Capture ? deviceId : nint.Zero);

        _device = Native.AllocateDevice();
        var result = Native.DeviceInit(nint.Zero, deviceConfig, _device);
        Native.Free(deviceConfig);

        if (result != Result.Success)
        {
            Native.Free(_device); 
            _device = nint.Zero;
            throw new InvalidOperationException($"Unable to init device. {result}");
        }

        result = Native.DeviceStart(_device);
        if (result != Result.Success)
        {
            CleanupCurrentDevice();
            throw new InvalidOperationException($"Unable to start device. {result}");
        }
        
        UpdateDevicesInfo();
        CurrentPlaybackDevice = PlaybackDevices.FirstOrDefault(x => x.IsDefault);
        CurrentCaptureDevice = CaptureDevices.FirstOrDefault(x => x.IsDefault);
    }

    private void CleanupCurrentDevice()
    {
        if (_device == nint.Zero) return;
        Native.DeviceStop(_device);
        Native.DeviceUninit(_device);
        Native.Free(_device);
        _device = nint.Zero;
    }


    private void AudioCallback(IntPtr _, IntPtr output, IntPtr input, uint length)
    {
        int sampleCount = (int)length * Channels;
        if (Capability != Capability.Record) ProcessGraph(output, sampleCount);
        if (Capability != Capability.Playback) ProcessAudioInput(input, sampleCount);
    }


    /// <inheritdoc />
    protected override void ProcessAudioData() { }

    /// <inheritdoc />
    protected override void CleanupAudioDevice()
    {
        CleanupCurrentDevice();
        Native.ContextUninit(_context);
        Native.Free(_context);
    }


    /// <inheritdoc />
    internal protected override ISoundEncoder CreateEncoder(string filePath, EncodingFormat encodingFormat,
        SampleFormat sampleFormat, int encodingChannels, int sampleRate)
    {
        return new MiniAudioEncoder(filePath, encodingFormat, sampleFormat, encodingChannels, sampleRate);
    }

    /// <inheritdoc />
    internal protected override ISoundDecoder CreateDecoder(Stream stream)
    {
        return new MiniAudioDecoder(stream);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        CleanupAudioDevice();
        base.Dispose(disposing);
    }

    /// <inheritdoc />
    public override void SwitchDevice(DeviceInfo deviceInfo, DeviceType type = DeviceType.Playback)
    {
        if (deviceInfo.Id == nint.Zero)
            throw new InvalidOperationException("Unable to switch playback device. Device ID is invalid.");

        InitializeDeviceInternal(deviceInfo.Id, type);
    }

    /// <inheritdoc />
    public override void UpdateDevicesInfo()
    {
        var result = Native.GetDevices(_context, out IntPtr pPlaybackDevices, out IntPtr pCaptureDevices,
            out IntPtr playbackDeviceCount, out IntPtr captureDeviceCount);
        if (result != Result.Success)
            throw new InvalidOperationException("Unable to get devices.");

        PlaybackDeviceCount = (int)playbackDeviceCount;
        CaptureDeviceCount = (int)captureDeviceCount;
        
        if (pPlaybackDevices == nint.Zero || pCaptureDevices == nint.Zero || playbackDeviceCount == 0 ||
            captureDeviceCount == 0)
            throw new InvalidOperationException("Unable to get devices.");

        PlaybackDevices = pPlaybackDevices.ReadArray<DeviceInfo>(PlaybackDeviceCount);
        CaptureDevices = pCaptureDevices.ReadArray<DeviceInfo>(CaptureDeviceCount);

        Native.Free(pPlaybackDevices);
        Native.Free(pCaptureDevices);
    }
}