using System.Collections.Generic;

namespace QwQ_Music.Services.Audio.Play;

/// <summary>
/// 效果器配置类
/// </summary>
public record EffectConfig
{
    public bool Enabled { get; set; }
    public Dictionary<string, object?> Parameters { get; set; } = new();
}
