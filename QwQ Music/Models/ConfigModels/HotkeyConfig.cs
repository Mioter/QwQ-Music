using System.Collections.Generic;
using System.Text.Json.Serialization;
using Avalonia.Input;
using QwQ_Music.Common.Services;

namespace QwQ_Music.Models.ConfigModels;

/// <summary>
///     可序列化的KeyGesture包装类
/// </summary>
public class SerializableKeyGesture(Key key, KeyModifiers modifiers = KeyModifiers.None)
{
    public SerializableKeyGesture()
        : this(Key.None)
    {
    }

    [JsonPropertyName("key")] public Key Key { get; set; } = key;

    [JsonPropertyName("modifiers")] public KeyModifiers KeyModifiers { get; set; } = modifiers;

    /// <summary>
    ///     转换为KeyGesture
    /// </summary>
    public KeyGesture ToKeyGesture()
    {
        return new KeyGesture(Key, KeyModifiers);
    }

    /// <summary>
    ///     从KeyGesture创建
    /// </summary>
    public static SerializableKeyGesture FromKeyGesture(KeyGesture gesture)
    {
        return new SerializableKeyGesture(gesture.Key, gesture.KeyModifiers);
    }

    /// <summary>
    ///     隐式转换操作符
    /// </summary>
    public static implicit operator KeyGesture(SerializableKeyGesture serializable)
    {
        return serializable.ToKeyGesture();
    }

    /// <summary>
    ///     隐式转换操作符
    /// </summary>
    public static implicit operator SerializableKeyGesture(KeyGesture gesture)
    {
        return FromKeyGesture(gesture);
    }
}

public class HotkeyConfig
{
    public bool IsEnableHotkey { get; set; } = true;

    public Dictionary<HotkeyFunction, List<SerializableKeyGesture>> FunctionToKeyMap { get; set; } =
        CreateDefaultHotkeyConfig();

    /// <summary>
    ///     创建默认热键配置
    /// </summary>
    /// <returns>默认热键配置</returns>
    internal static Dictionary<HotkeyFunction, List<SerializableKeyGesture>> CreateDefaultHotkeyConfig()
    {
        return new Dictionary<HotkeyFunction, List<SerializableKeyGesture>>
        {
            // 上一首 - 支持媒体键和自定义热键
            [HotkeyFunction.PreviousSong] =
            [
                new SerializableKeyGesture(Key.MediaPreviousTrack),
                new SerializableKeyGesture(Key.Left, KeyModifiers.Control),
            ],

            // 下一首 - 支持媒体键和自定义热键
            [HotkeyFunction.NextSong] =
            [
                new SerializableKeyGesture(Key.MediaNextTrack),
                new SerializableKeyGesture(Key.Right, KeyModifiers.Control),
            ],

            // 播放/暂停 - 支持媒体键和自定义热键
            [HotkeyFunction.PlayPause] =
            [
                new SerializableKeyGesture(Key.MediaPlayPause),
                new SerializableKeyGesture(Key.Space, KeyModifiers.Control),
            ],

            // 音量控制
            [HotkeyFunction.VolumeUp] = [new SerializableKeyGesture(Key.Up, KeyModifiers.Control)],
            [HotkeyFunction.VolumeDown] = [new SerializableKeyGesture(Key.Down, KeyModifiers.Control)],

            // 其他功能
            [HotkeyFunction.ToggleMute] = [new SerializableKeyGesture(Key.M, KeyModifiers.Control)],
            [HotkeyFunction.TogglePlayMode] = [new SerializableKeyGesture(Key.R, KeyModifiers.Control)],
            [HotkeyFunction.RefreshCurrentMusic] = [new SerializableKeyGesture(Key.F5)],
            [HotkeyFunction.ShowPlaylistInfo] = [new SerializableKeyGesture(Key.L, KeyModifiers.Control)],
            [HotkeyFunction.ShowCurrentInfo] = [new SerializableKeyGesture(Key.F1)],
        };
    }
}
