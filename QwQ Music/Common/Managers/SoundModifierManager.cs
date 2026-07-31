using Avalonia.Collections;
using QwQ_Music.Common.Interfaces;
using QwQ_Music.Models.ConfigModels;

namespace QwQ_Music.Common.Managers;

public class SoundModifierManager {
    private readonly Dictionary<string, ISoundModifierModel> _modifierMap = new();
    public readonly SoundEffectConfig SoundEffectConfig = ConfigManager.SoundModifierConfig.SoundEffectConfig;

    private SoundModifierManager() { Initialize(); }

    public static SoundModifierManager Default { get; } = new();

    public AvaloniaList<ISoundModifierModel> SoundModifiers { get; } = [];

    private void Initialize() {
        // 确保内置效果已初始化
        if (SoundEffectConfig.BuiltInSoundEffects.Count == 0) {
            var defaults = new (string name, bool enabled)[] {
                ("AlgorithmicReverb", true), ("BassBooster", true), ("Chorus", true), ("Compressor", false),
                ("Delay", false), ("FrequencyBand", false), ("ParametricEqualizer", false), ("TrebleBooster", false)
            };

            foreach ((string name, bool enabled) in defaults)
                SoundEffectConfig.BuiltInSoundEffects[name] = enabled;
        }

        // 加载所有启用的效果
        foreach (KeyValuePair<string, bool> kvp in SoundEffectConfig.BuiltInSoundEffects.Where(kvp => kvp.Value))
            LoadModifierInternal(kvp.Key);
    }

    public void Clear() {
        SoundModifiers.Clear();
        _modifierMap.Clear();
    }

    private void LoadModifierInternal(string modifierName) {
        if (_modifierMap.ContainsKey(modifierName))
            return; // 已加载

        ISoundModifierModel? model = modifierName switch {
            "AlgorithmicReverb" => SoundEffectConfig.AlgorithmicReverb,
            "BassBooster"       => SoundEffectConfig.BassBooster,
            "Chorus"            => SoundEffectConfig.Chorus,
            "Compressor"        => SoundEffectConfig.Compressor,
            "Delay"             => SoundEffectConfig.Delay,
            // TODO: 补充其他效果的映射，暂时这样吧，现在懒得写了
            _ => null
        };

        if (model == null)
            return;
        _modifierMap[modifierName] = model;
        SoundModifiers.Add(model);
    }

    private void UnloadModifierInternal(string modifierName) {
        if (_modifierMap.Remove(modifierName, out ISoundModifierModel? model))
            SoundModifiers.Remove(model);
    }

    public void LoadModifier(string modifierName) {
        if (!SoundEffectConfig.BuiltInSoundEffects.ContainsKey(modifierName))
            return;

        SoundEffectConfig.BuiltInSoundEffects[modifierName] = true;
        LoadModifierInternal(modifierName);
    }

    public void UnLoadModifier(string modifierName) {
        if (!SoundEffectConfig.BuiltInSoundEffects.ContainsKey(modifierName))
            return;

        SoundEffectConfig.BuiltInSoundEffects[modifierName] = false;
        UnloadModifierInternal(modifierName);
    }
}