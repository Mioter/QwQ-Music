using SoundFlow.Abstracts;
using SoundFlow.Interfaces;

namespace SoundFlow.Components;

/// <summary>
/// A sound player that plays audio from a data provider.
/// </summary>
public sealed class SoundPlayer(ISoundDataProvider dataProvider) : SoundPlayerBase(dataProvider)
{
    /// <inheritdoc />
    public override string Name { get; set; } = "Sound Player";
}
