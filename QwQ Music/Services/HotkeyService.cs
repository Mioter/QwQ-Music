using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Input;
using QwQ_Music.ViewModels;

namespace QwQ_Music.Services;

/// <summary>
/// 热键服务，用于管理全局热键
/// </summary>
public class HotkeyService
{
    private readonly Dictionary<KeyGesture, Action> _hotkeyActions = new();
    private readonly MusicPlayerViewModel _musicPlayerViewModel;

    public HotkeyService(MusicPlayerViewModel musicPlayerViewModel)
    {
        _musicPlayerViewModel = musicPlayerViewModel;
        RegisterDefaultHotkeys();
    }

    /// <summary>
    /// 注册默认热键
    /// </summary>
    private void RegisterDefaultHotkeys()
    {
        // 媒体键 - 上一首
        RegisterHotkey(
            new KeyGesture(Key.MediaPreviousTrack),
            () => _musicPlayerViewModel.TogglePreviousSongCommand.Execute(null)
        );

        // 媒体键 - 下一首
        RegisterHotkey(
            new KeyGesture(Key.MediaNextTrack),
            () => _musicPlayerViewModel.ToggleNextSongCommand.Execute(null)
        );

        // 媒体键 - 播放/暂停
        RegisterHotkey(
            new KeyGesture(Key.MediaPlayPause),
            () => _musicPlayerViewModel.TogglePlaybackCommand.Execute(null)
        );

        // 自定义热键 - Ctrl+Left 上一首
        RegisterHotkey(
            new KeyGesture(Key.Left, KeyModifiers.Control),
            () => _musicPlayerViewModel.TogglePreviousSongCommand.Execute(null)
        );

        // 自定义热键 - Ctrl+Right 下一首
        RegisterHotkey(
            new KeyGesture(Key.Right, KeyModifiers.Control),
            () => _musicPlayerViewModel.ToggleNextSongCommand.Execute(null)
        );

        // 自定义热键 - Ctrl+Space 播放/暂停
        RegisterHotkey(
            new KeyGesture(Key.Space, KeyModifiers.Control),
            () => _musicPlayerViewModel.TogglePlaybackCommand.Execute(null)
        );
    }

    /// <summary>
    /// 注册热键
    /// </summary>
    /// <param name="gesture">按键组合</param>
    /// <param name="action">执行的操作</param>
    public void RegisterHotkey(KeyGesture gesture, Action action)
    {
        _hotkeyActions[gesture] = action;
    }

    /// <summary>
    /// 处理按键事件
    /// </summary>
    /// <param name="e">按键事件参数</param>
    /// <returns>是否处理了热键</returns>
    public bool HandleKeyDown(KeyEventArgs e)
    {
        foreach (var hotkey in _hotkeyActions.Keys.Where(hotkey => hotkey.Matches(e)))
        {
            _hotkeyActions[hotkey].Invoke();
            e.Handled = true;
            return true;
        }
        return false;
    }
}
