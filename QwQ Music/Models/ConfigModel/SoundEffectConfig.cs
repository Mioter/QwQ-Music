using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using QwQ_Music.Models.ModelBase;
using QwQ_Music.Models.SoundEffectModels;
using QwQ_Music.Services.Audio.Play;

namespace QwQ_Music.Models.ConfigModel;

public class SoundEffectConfig : ObservableObject, IDisposable
{
    public SoundEffectConfig()
    {
        EffectModelBase.ParameterChanged += ParameterChanged;
    }

    private void ParameterChanged((string, string, object) obj) => UpdateEffectConfig(obj.Item1, obj.Item2, obj.Item3);

    public readonly Dictionary<string, EffectConfig> UserConfigs = new(); // 用户配置存储

    public StereoEnhancementModel StereoEnhancement { get; set; } = new();

    public ReplayGainModel ReplayGain { get; set; } = new();

    public FadeModel Fade { get; set; } = new();

    public ReverbModel Reverb { get; set; } = new();

    public CompressorModel Compressor { get; set; } = new();

    public DelayModel Delay { get; set; } = new();

    public DistortionModel Distortion { get; set; } = new();

    public TremoloModel Tremolo { get; set; } = new();

    public EqualizerModel Equalizer { get; set; } = new();

    public RotatingModel Rotating { get; set; } = new();

    public SpatialModel Spatial { get; set; } = new();

    private MusicReplayGainStandard _selectedMusicReplayGainStandard = MusicReplayGainStandard.Streaming;
    public MusicReplayGainStandard SelectedMusicReplayGainStandard
    {
        get => _selectedMusicReplayGainStandard;
        set => SetProperty(ref _selectedMusicReplayGainStandard, value);
    }

    private double _customMusicReplayGainStandard = 12;
    public double CustomMusicReplayGainStandard
    {
        get => _customMusicReplayGainStandard;
        set => SetProperty(ref _customMusicReplayGainStandard, value);
    }

    private void UpdateEffectConfig(string effectName, string parameter, object value)
    {
        if (!UserConfigs.ContainsKey(effectName))
            UserConfigs[effectName] = new EffectConfig();

        UserConfigs.TryGetValue(effectName, out var effectConfig);

        if (effectConfig == null)
            return;

        switch (parameter.ToLower())
        {
            case "enabled":
                if (value is bool isEnabled)
                {
                    effectConfig.Enabled = isEnabled;
                    _audioPlay?.UpdateEffectsEnabled(effectName, isEnabled);
                }
                break;
            default:
                effectConfig.Parameters[parameter] = value;
                _audioPlay?.UpdateEffectsParameters(effectName, parameter, value);
                break;
        }
    }

    private AudioPlay? _audioPlay;

    public void Initialization(AudioPlay audioPlay)
    {
        _audioPlay = audioPlay;
        _audioPlay.UserConfigs = UserConfigs;
    }

    private void ReleaseUnmanagedResources()
    {
        EffectModelBase.ParameterChanged -= ParameterChanged;
    }

    private void Dispose(bool disposing)
    {
        ReleaseUnmanagedResources();
        if (disposing)
        {
            _audioPlay?.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~SoundEffectConfig()
    {
        Dispose(false);
    }
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(SoundEffectConfig))]
internal partial class SoundEffectConfigModelJsonSerializerContext : JsonSerializerContext;
