using System.Collections.Specialized;
using System.Text.Json.Serialization;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using QwQ_Music.Common.Interfaces;
using SoundFlow.Abstracts;
using SoundFlow.Enums;
using SoundFlow.Modifiers;
using SoundFlow.Structs;

namespace QwQ_Music.Models.SoundEffectModels;

public class ParametricEqualizerModel : ObservableObject, ISoundModifierModel<ParametricEqualizer> {
    /// <summary>
    ///     获取或设置均衡器频段列表。
    /// </summary>
    public AvaloniaList<EqualizerBand> Bands {
        get;
        set {
            if (!SetProperty(ref field, value))
                return;

            // 当频段集合发生变化时，更新修饰器
            UpdateModifierBands();

            // 订阅集合变化事件
            field.CollectionChanged += OnBandsCollectionChanged;
        }
    } = [];

    [JsonIgnore]
    public ParametricEqualizer? Modifier { get; private set; }

    [JsonIgnore]
    SoundModifier? ISoundModifierModel.Modifier => Modifier;

    [JsonIgnore]
    public string Name { get; } = "参数均衡器";

    public void Initialize(AudioFormat audioFormat) {
        var modifier = new ParametricEqualizer(audioFormat) { Enabled = Enabled };

        // 添加所有已配置的频段
        foreach (EqualizerBand? band in Bands)
            modifier.AddBand(band);

        Modifier = modifier;
    }

    public void Revoke() { Modifier = null; }

    /// <summary>
    ///     获取或设置是否启用效果器。
    /// </summary>
    public bool Enabled {
        get;
        set {
            if (SetProperty(ref field, value))
                Modifier?.Enabled = value;
        }
    } = true;

    /// <summary>
    ///     当频段集合发生变化时调用。
    /// </summary>
    private void OnBandsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) { UpdateModifierBands(); }

    /// <summary>
    ///     更新修饰器的频段列表。
    /// </summary>
    private void UpdateModifierBands() {
        if (Modifier == null)
            return;

        // 清空现有频段并重新添加所有频段
        Modifier.Bands.Clear();
        foreach (EqualizerBand? band in Bands)
            Modifier.AddBand(band);
    }

    /// <summary>
    ///     添加一个均衡器频段。
    /// </summary>
    /// <param name="band">要添加的频段。</param>
    public void AddBand(EqualizerBand band) {
        Bands.Add(band);
        // CollectionChanged 事件会自动处理更新修饰器
    }

    /// <summary>
    ///     添加多个均衡器频段。
    /// </summary>
    /// <param name="bands">要添加的频段集合。</param>
    public void AddBands(IEnumerable<EqualizerBand> bands) {
        foreach (EqualizerBand band in bands)
            Bands.Add(band);
    }

    /// <summary>
    ///     移除一个均衡器频段。
    /// </summary>
    /// <param name="band">要移除的频段。</param>
    public void RemoveBand(EqualizerBand band) { Bands.Remove(band); }

    /// <summary>
    ///     根据索引移除均衡器频段。
    /// </summary>
    /// <param name="index">要移除的频段索引。</param>
    public void RemoveBandAt(int index) {
        if (index >= 0 && index < Bands.Count)
            Bands.RemoveAt(index);
    }

    /// <summary>
    ///     清空所有均衡器频段。
    /// </summary>
    public void ClearBands() { Bands.Clear(); }

    /// <summary>
    ///     创建一个预设的均衡器配置（示例）。
    /// </summary>
    public void ApplyPreset(string presetName) {
        ClearBands();

        switch (presetName.ToLower()) {
            case "flat":
                // 平坦响应，不添加任何频段
                break;

            case "bass boost":
                AddBand(new EqualizerBand(FilterType.LowShelf, 80f, 6f, 0.7f));
                break;

            case "treble boost":
                AddBand(new EqualizerBand(FilterType.HighShelf, 10000f, 6f, 0.7f));
                break;

            case "vocal boost":
                AddBand(new EqualizerBand(FilterType.Peaking, 1000f, 3f, 1.5f));
                AddBand(new EqualizerBand(FilterType.Peaking, 3000f, 2f, 2f));
                break;

            case "rock":
                AddBand(new EqualizerBand(FilterType.LowShelf, 80f, 4f, 0.7f));
                AddBand(new EqualizerBand(FilterType.Peaking, 800f, 2f, 1.5f));
                AddBand(new EqualizerBand(FilterType.Peaking, 2500f, 3f, 1.5f));
                AddBand(new EqualizerBand(FilterType.HighShelf, 8000f, 2f, 0.7f));
                break;
        }
    }
}