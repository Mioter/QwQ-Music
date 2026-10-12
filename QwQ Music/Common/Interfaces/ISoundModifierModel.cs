using SoundFlow.Abstracts;
using SoundFlow.Structs;

namespace QwQ_Music.Common.Interfaces;

public interface ISoundModifierModel {
    public SoundModifier? Modifier { get; }

    public string Name { get; }

    public bool Enabled { get; set; }

    public void Initialize(AudioFormat modifier);

    public void Revoke();
}

public interface ISoundModifierModel<out TModifier> : ISoundModifierModel where TModifier : SoundModifier {
    public new TModifier? Modifier { get; }
}