namespace QwQ_Music.Models;

public class ConfigInfoModel
{
    public PlayerConfig? PlayerConfig { get; init; }
    
    public SoundEffectConfigModel? SoundEffectConfig { get; init; }
}

public class PlayerConfig
{
    public int Volume { get; init; }
}

