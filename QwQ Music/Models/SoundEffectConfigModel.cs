using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.CustomControls;
using QwQ_Music.Services.Audio.Play;

namespace QwQ_Music.Models;

public partial class SoundEffectConfigModel : ObservableObject
{
    #region 立体效果

    private bool _isEnabledStereoEnhancementEffect = true;
    public bool IsEnabledStereoEnhancementEffect
    {
        get => _isEnabledStereoEnhancementEffect;
        set
        {
            if (SetProperty(ref _isEnabledStereoEnhancementEffect, value))
            {
                UpdateStereoEnhancementEffectConfig();
            }
        }
    }

    private float _stereoEnhancementFactor = 1.5f;
    public float StereoEnhancementFactor
    {
        get => _stereoEnhancementFactor;
        set
        {
            if (SetProperty(ref _stereoEnhancementFactor, value))
            {
                UpdateStereoEnhancementEffectConfig();
            }
        }
    }

    private float _stereoDynamicRangeCompression = 0.5f;
    public float StereoDynamicRangeCompression
    {
        get => _stereoDynamicRangeCompression;
        set
        {
            if (SetProperty(ref _stereoDynamicRangeCompression, value))
            {
                UpdateStereoEnhancementEffectConfig();
            }
        }
    }

    private bool _isBassMixing;
    public bool IsBassMixing
    {
        get => _isBassMixing;
        set
        {
            if (SetProperty(ref _isBassMixing, value))
            {
                UpdateStereoEnhancementEffectConfig();
            }
        }
    }

    private float _stereoStereoWidth = 1.0f;
    public float StereoStereoWidth
    {
        get => _stereoStereoWidth;
        set
        {
            if (SetProperty(ref _stereoStereoWidth, value))
            {
                UpdateStereoEnhancementEffectConfig();
            }
        }
    }

    private float _stereoHighFrequencyBoost = 1.0f;
    public float StereoHighFrequencyBoost
    {
        get => _stereoHighFrequencyBoost;
        set
        {
            if (SetProperty(ref _stereoHighFrequencyBoost, value))
            {
                UpdateStereoEnhancementEffectConfig();
            }
        }
    }

    private void UpdateStereoEnhancementEffectConfig()
    {
        _audioPlay?.UpdateSpecificEffects(
            "Stereo Enhancement",
            new EffectConfig
            {
                Enabled = IsEnabledStereoEnhancementEffect,
                Parameters = new Dictionary<string, object?>
                {
                    { "EnhancementFactor", StereoEnhancementFactor },
                    { "StereoWidth", StereoStereoWidth },
                    { "HighFrequencyBoost", StereoHighFrequencyBoost },
                    { "BassMixing", IsBassMixing },
                    { "DynamicRangeCompression", StereoDynamicRangeCompression },
                },
            }
        );
    }

    #endregion

    #region 回调增益

    private bool _isEnabledReplayGainEffect = true;
    public bool IsEnabledReplayGainEffect
    {
        get => _isEnabledReplayGainEffect;
        set
        {
            if (SetProperty(ref _isEnabledReplayGainEffect, value))
            {
                UpdateReplayGainEffectConfig();
            }
        }
    }

    private void UpdateReplayGainEffectConfig()
    {
        _audioPlay?.UpdateSpecificEffects("Replay Gain", new EffectConfig { Enabled = IsEnabledReplayGainEffect });
    }

    #endregion

    #region 淡入淡出

    private bool _isEnableFadeEffect = true;
    public bool IsEnableFadeEffect
    {
        get => _isEnableFadeEffect;
        set
        {
            if (SetProperty(ref _isEnableFadeEffect, value))
            {
                UpdateFadeEffectConfig();
            }
        }
    }

    private double _fadeInMilliseconds = 1000f;
    public double FadeInMilliseconds
    {
        get => _fadeInMilliseconds;
        set
        {
            if (SetProperty(ref _fadeInMilliseconds, value))
            {
                UpdateFadeEffectConfig();
            }
        }
    }

    private double _fadeOutMilliseconds = 1000f;
    public double FadeOutMilliseconds
    {
        get => _fadeOutMilliseconds;
        set
        {
            if (SetProperty(ref _fadeOutMilliseconds, value))
            {
                UpdateFadeEffectConfig();
            }
        }
    }

    private void UpdateFadeEffectConfig()
    {
        _audioPlay?.UpdateSpecificEffects(
            "Fade",
            new EffectConfig
            {
                Enabled = IsEnableFadeEffect,
                Parameters = new Dictionary<string, object?>
                {
                    { "FadeInMilliseconds", FadeInMilliseconds },
                    { "FadeOutMilliseconds", FadeOutMilliseconds },
                },
            }
        );
    }

    #endregion

    #region 混响

    private bool _isEnableReverbEffect;
    public bool IsEnableReverbEffect
    {
        get => _isEnableReverbEffect;
        set
        {
            if (SetProperty(ref _isEnableReverbEffect, value))
            {
                UpdateReverbEffectConfig();
            }
        }
    }

    private float _reverbRoomSize;
    public float ReverbRoomSize
    {
        get => _reverbRoomSize;
        set
        {
            if (SetProperty(ref _reverbRoomSize, value))
            {
                UpdateReverbEffectConfig();
            }
        }
    }

    private float _reverbDryMix;
    public float ReverbDryMix
    {
        get => _reverbDryMix;
        set
        {
            if (SetProperty(ref _reverbDryMix, value))
            {
                UpdateReverbEffectConfig();
            }
        }
    }

    private float _reverbWetMix;
    public float ReverbWetMix
    {
        get => _reverbWetMix;
        set
        {
            if (SetProperty(ref _reverbWetMix, value))
            {
                UpdateReverbEffectConfig();
            }
        }
    }

    private float _reverbDampening;
    public float ReverbDampening
    {
        get => _reverbDampening;
        set
        {
            if (SetProperty(ref _reverbDampening, value))
            {
                UpdateReverbEffectConfig();
            }
        }
    }

    private float _reverbDecayTime;
    public float ReverbDecayTime
    {
        get => _reverbDecayTime;
        set
        {
            if (SetProperty(ref _reverbDecayTime, value))
            {
                UpdateReverbEffectConfig();
            }
        }
    }

    private float _reverbPreDelayMs;
    public float ReverbPreDelayMs
    {
        get => _reverbPreDelayMs;
        set
        {
            if (SetProperty(ref _reverbPreDelayMs, value))
            {
                UpdateReverbEffectConfig();
            }
        }
    }

    private void UpdateReverbEffectConfig()
    {
        _audioPlay?.UpdateSpecificEffects(
            "Reverb",
            new EffectConfig
            {
                Enabled = IsEnableReverbEffect,
                Parameters = new Dictionary<string, object?>
                {
                    { "RoomSize", ReverbRoomSize },
                    { "DryMix", ReverbDryMix },
                    { "WetMix", ReverbWetMix },
                    { "Dampening", ReverbDampening },
                    { "DecayTime", ReverbDecayTime },
                    { "PreDelay", ReverbPreDelayMs },
                },
            }
        );
    }

    #endregion

    #region 压缩器

    private bool _isEnableCompressorEffect = true;
    public bool IsEnableCompressorEffect
    {
        get => _isEnableCompressorEffect;
        set
        {
            if (SetProperty(ref _isEnableCompressorEffect, value))
            {
                UpdateCompressorEffectConfig();
            }
        }
    }

    private void UpdateCompressorEffectConfig()
    {
        _audioPlay?.UpdateSpecificEffects("Compressor", new EffectConfig { Enabled = IsEnableCompressorEffect });
    }

    #endregion

    #region 回声

    private bool _isEnableEchoesEffect;
    public bool IsEnableEchoesEffect
    {
        get => _isEnableEchoesEffect;
        set
        {
            if (SetProperty(ref _isEnableEchoesEffect, value))
            {
                UpdateDelayEffectConfig();
            }
        }
    }

    private void UpdateDelayEffectConfig()
    {
        _audioPlay?.UpdateSpecificEffects("Echoes", new EffectConfig { Enabled = IsEnableEchoesEffect });
    }

    #endregion

    #region 失真

    private bool _isEnableDistortionEffect;
    public bool IsEnableDistortionEffect
    {
        get => _isEnableDistortionEffect;
        set
        {
            if (SetProperty(ref _isEnableDistortionEffect, value))
            {
                UpdateDistortionEffectConfig();
            }
        }
    }

    private void UpdateDistortionEffectConfig()
    {
        _audioPlay?.UpdateSpecificEffects("Distortion", new EffectConfig { Enabled = IsEnableDistortionEffect });
    }

    #endregion

    #region 颤音

    private bool _isEnableTremoloEffect;
    public bool IsEnableTremoloEffect
    {
        get => _isEnableTremoloEffect;
        set
        {
            if (SetProperty(ref _isEnableTremoloEffect, value))
            {
                UpdateTremoloEffectConfig();
            }
        }
    }

    private void UpdateTremoloEffectConfig()
    {
        _audioPlay?.UpdateSpecificEffects("Tremolo", new EffectConfig { Enabled = IsEnableTremoloEffect });
    }

    #endregion

    #region 均衡器

    private bool _isEnabledEqualizerEffect;
    public bool IsEnabledEqualizerEffect
    {
        get => _isEnabledEqualizerEffect;
        set
        {
            if (SetProperty(ref _isEnabledEqualizerEffect, value))
            {
                UpdateEqualizerEffectConfig();
            }
        }
    }

    public ObservableCollection<EqualizerConfigModel> EqualizerConfigs { get; set; } =
        [
            new() { FrequencyBand = 31.25f, GainValue = 0 },
            new() { FrequencyBand = 62.5f, GainValue = 0 },
            new() { FrequencyBand = 125f, GainValue = 0 }, // 低频段
            new() { FrequencyBand = 250f, GainValue = 0 },
            new() { FrequencyBand = 500f, GainValue = 0 },
            new() { FrequencyBand = 1000f, GainValue = 0 },
            new() { FrequencyBand = 2000f, GainValue = 0 }, // 中频段
            new() { FrequencyBand = 4000f, GainValue = 0 },
            new() { FrequencyBand = 8000f, GainValue = 0 },
            new() { FrequencyBand = 16000f, GainValue = 0 }, // 高频段
        ];

    private void UpdateEqualizerEffectConfig()
    {
        var bands = EqualizerConfigs
            .Select(config => (frequency: config.FrequencyBand, gain: (float)config.GainValue))
            .ToArray();

        _audioPlay?.UpdateSpecificEffects(
            "Equalizer",
            new EffectConfig
            {
                Enabled = IsEnabledEqualizerEffect,
                Parameters = new Dictionary<string, object?> { { "Bands", bands } },
            }
        );
    }

    #endregion

    #region 环绕

    private bool _isEnableRotatingEffect;
    public bool IsEnableRotatingEffect
    {
        get => _isEnableRotatingEffect;
        set
        {
            if (SetProperty(ref _isEnableRotatingEffect, value))
            {
                UpdateRotatingEffectConfig();
            }
        }
    }

    private float _rotatingRotationSpeed = 1f;
    public float RotatingRotationSpeed
    {
        get => _rotatingRotationSpeed;
        set
        {
            if (SetProperty(ref _rotatingRotationSpeed, value))
            {
                UpdateRotatingEffectConfig();
            }
        }
    }

    private bool _rotatingIsClockwise = true;
    public bool RotatingIsClockwise
    {
        get => _rotatingIsClockwise;
        set
        {
            if (SetProperty(ref _rotatingIsClockwise, value))
            {
                UpdateRotatingEffectConfig();
            }
        }
    }

    private float _rotatingRadius = 50f;
    public float RotatingRadius
    {
        get => _rotatingRadius;
        set
        {
            if (SetProperty(ref _rotatingRadius, value))
            {
                UpdateRotatingEffectConfig();
            }
        }
    }

    private void UpdateRotatingEffectConfig()
    {
        _audioPlay?.UpdateSpecificEffects(
            "Rotating",
            new EffectConfig
            {
                Enabled = IsEnableRotatingEffect,
                Parameters = new Dictionary<string, object?>
                {
                    { "RotationSpeed", RotatingRotationSpeed },
                    { "Radius", RotatingRadius },
                    { "IsClockwise", RotatingIsClockwise },
                },
            }
        );
    }

    #endregion

    #region 空间音频

    private bool _isEnableSpatialEffect;
    public bool IsEnableSpatialEffect
    {
        get => _isEnableSpatialEffect;
        set
        {
            if (SetProperty(ref _isEnableSpatialEffect, value))
            {
                UpdateSpatialEffectConfig();
            }
        }
    }

    private float _spatialAngle;
    public float SpatialAngle
    {
        get => _spatialAngle;
        set
        {
            if (SetProperty(ref _spatialAngle, value))
            {
                UpdateSpatialEffectConfig();
            }
        }
    }

    private float _spatialDistance;
    public float SpatialDistance
    {
        get => _spatialDistance;
        set
        {
            if (SetProperty(ref _spatialDistance, value))
            {
                UpdateSpatialEffectConfig();
            }
        }
    }

    private void UpdateSpatialEffectConfig()
    {
        _audioPlay?.UpdateSpecificEffects(
            "Spatial",
            new EffectConfig
            {
                Enabled = IsEnableSpatialEffect,
                Parameters = new Dictionary<string, object?>
                {
                    { "Angle", SpatialAngle },
                    { "Distance", SpatialDistance },
                },
            }
        );
    }

    #endregion


    private AudioPlay? _audioPlay;

    [RelayCommand]
    private void RestoreDefaultEqualizerVolume()
    {
        foreach (var config in EqualizerConfigs)
        {
            config.GainValue = 0.0f;
        }

        UpdateEqualizerEffectConfig();
    }

    [RelayCommand]
    private void EqualizerVolumeChanged()
    {
        UpdateEqualizerEffectConfig();
    }

    [RelayCommand]
    private void OnSpeakerPositionChanged(PositionChangedEventArgs e)
    {
        SpatialAngle = (float)e.Angle;
        SpatialDistance = (float)e.Distance;
    }

    public void SetAudioPlay(AudioPlay? audioPlay)
    {
        _audioPlay = audioPlay;
    }

    public void UpdateAllEffectsConfig()
    {
        UpdateStereoEnhancementEffectConfig();
        UpdateReplayGainEffectConfig();
        UpdateFadeEffectConfig();
        UpdateReverbEffectConfig();
        UpdateCompressorEffectConfig();
        UpdateDelayEffectConfig();
        UpdateDistortionEffectConfig();
        UpdateTremoloEffectConfig();
        UpdateEqualizerEffectConfig();
        UpdateRotatingEffectConfig();
        UpdateSpatialEffectConfig();
    }
}

public class EqualizerConfigModel : ObservableObject
{
    public float FrequencyBand { get; init; }

    private double _gainValue;
    public double GainValue
    {
        get => _gainValue;
        set => SetProperty(ref _gainValue, value);
    }
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(SoundEffectConfigModel))]
internal partial class SoundEffectConfigModelJsonSerializerContext : JsonSerializerContext;
