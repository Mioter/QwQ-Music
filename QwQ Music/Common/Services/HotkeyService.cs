using Avalonia.Input;
using QwQ_Music.Common.Managers;
using QwQ_Music.Models.ConfigModels;

namespace QwQ_Music.Common.Services;

/// <summary>
///     热键功能枚举
/// </summary>
public enum HotkeyFunction {
    /// <summary>
    ///     上一首
    /// </summary>
    Previous,

    /// <summary>
    ///     下一首
    /// </summary>
    Next,

    /// <summary>
    ///     播放/暂停
    /// </summary>
    TogglePlay,

    /// <summary>
    ///     静音切换
    /// </summary>
    ToggleMute,

    /// <summary>
    ///     播放模式切换
    /// </summary>
    SwitchPlayMode,

    /// <summary>
    ///     音量增加
    /// </summary>
    VolumeUp,

    /// <summary>
    ///     音量减少
    /// </summary>
    VolumeDown,

    /// <summary>
    ///     刷新当前音乐
    /// </summary>
    Replay,

    /// <summary>
    ///     显示播放列表信息
    /// </summary>
    ShowPlaylistInfo,

    /// <summary>
    ///     显示当前播放信息
    /// </summary>
    ShowAudioInfo,

    /// <summary>
    ///     页面前进
    /// </summary>
    NextPage,

    /// <summary>
    ///     页面后退
    /// </summary>
    PrevPage
}

/// <summary>
///     热键服务，用于管理全局热键
/// </summary>
public static class HotkeyService {
    private static readonly HotkeyConfig _hotkeyConfig = ConfigManager.HotkeyConfig;
    private static readonly Dictionary<HotkeyFunction, Action> _functionToActionMap = [];

    private static Dictionary<HotkeyFunction, List<SerializableKeyGesture>> FunctionToKeyMap =>
        _hotkeyConfig.FunctionToKeyMap;

    private static bool IsEnable => _hotkeyConfig.IsEnableHotkey;

    /// <summary>
    ///     注册功能委托
    /// </summary>
    /// <param name="function">功能枚举</param>
    /// <param name="action">要执行的委托</param>
    public static void RegisterFunctionAction(HotkeyFunction function, Action action) {
        _functionToActionMap[function] = action;
    }

    /// <summary>
    ///     注销功能委托
    /// </summary>
    /// <param name="function">功能枚举</param>
    public static void UnregisterFunctionAction(HotkeyFunction function) { _functionToActionMap.Remove(function); }

    /// <summary>
    ///     注销所有功能委托
    /// </summary>
    public static void UnregisterAllFunctionActions() { _functionToActionMap.Clear(); }

    /// <summary>
    ///     注册热键
    /// </summary>
    /// <param name="function">功能枚举</param>
    /// <param name="gesture">按键组合</param>
    /// <returns>是否注册成功</returns>
    public static bool RegisterHotkey(HotkeyFunction function, KeyGesture gesture) {
        // 确保功能对应的列表存在并添加按键
        if (!FunctionToKeyMap.TryGetValue(function, out List<SerializableKeyGesture>? gestures)) {
            gestures = [];
            FunctionToKeyMap[function] = gestures;
        }

        // 避免重复添加
        if (gestures.Any(g => g.ToKeyGesture().Equals(gesture)))
            return false;

        gestures.Add(SerializableKeyGesture.FromKeyGesture(gesture));

        return true;
    }

    /// <summary>
    ///     修改热键（替换指定功能的所有按键）
    /// </summary>
    /// <param name="function">功能枚举</param>
    /// <param name="newGestures">新的按键组合列表</param>
    /// <returns>是否修改成功</returns>
    public static bool ModifyHotkey(HotkeyFunction function, params IEnumerable<KeyGesture> newGestures) {
        FunctionToKeyMap[function] = new List<SerializableKeyGesture>(
            newGestures.Select(SerializableKeyGesture.FromKeyGesture));

        return true;
    }

    /// <summary>
    ///     修改热键（替换指定功能的单个按键）
    /// </summary>
    /// <param name="function">功能枚举</param>
    /// <param name="oldGesture">原按键组合</param>
    /// <param name="newGesture">新的按键组合</param>
    /// <returns>是否修改成功</returns>
    public static bool ModifyHotkey(HotkeyFunction function, KeyGesture oldGesture, KeyGesture newGesture) {
        if (!FunctionToKeyMap.TryGetValue(function, out List<SerializableKeyGesture>? gestures))
            return false;

        int index = gestures.FindIndex(g => g.ToKeyGesture().Equals(oldGesture));

        if (index == -1)
            return false;

        gestures[index] = SerializableKeyGesture.FromKeyGesture(newGesture);

        return true;
    }

    /// <summary>
    ///     删除热键（删除指定功能的所有按键）
    /// </summary>
    /// <param name="function">功能枚举</param>
    /// <returns>是否删除成功</returns>
    public static bool RemoveHotkey(HotkeyFunction function) { return FunctionToKeyMap.Remove(function); }

    /// <summary>
    ///     删除热键（删除指定功能的特定按键）
    /// </summary>
    /// <param name="function">功能枚举</param>
    /// <param name="gesture">要删除的按键组合</param>
    /// <returns>是否删除成功</returns>
    public static bool RemoveHotkey(HotkeyFunction function, KeyGesture? gesture) {
        if (!FunctionToKeyMap.TryGetValue(function, out List<SerializableKeyGesture>? gestures))
            return false;
        if (gesture is not null)
            return gestures.RemoveAll(g => g.ToKeyGesture().Equals(gesture)) > 0;
        gestures.Clear();
        return true;
    }

    /// <summary>
    ///     检查特定按键是否已注册
    /// </summary>
    /// <param name="function">功能枚举</param>
    /// <param name="gesture">按键组合</param>
    /// <returns>是否已注册</returns>
    public static bool HasHotkey(HotkeyFunction function, KeyGesture gesture) {
        return FunctionToKeyMap.TryGetValue(function, out List<SerializableKeyGesture>? gestures) &&
               gestures.Any(g => g.ToKeyGesture().Equals(gesture));
    }

    /// <summary>
    ///     获取功能对应的所有按键
    /// </summary>
    /// <param name="function">功能枚举</param>
    /// <returns>按键组合列表，如果不存在则返回空列表</returns>
    public static List<KeyGesture> GetHotkeys(HotkeyFunction function) {
        return FunctionToKeyMap.GetValueOrDefault(function, []).Select(g => g.ToKeyGesture()).ToList();
    }

    /// <summary>
    ///     获取所有功能的描述
    /// </summary>
    /// <returns>功能到描述的映射</returns>
    public static Dictionary<HotkeyFunction, string> GetAllFunctionDescriptions() {
        return Enum.GetValues<HotkeyFunction>().ToDictionary(f => f, GetFunctionDescription);
    }

    /// <summary>
    ///     重置为默认热键
    /// </summary>
    public static void ResetToDefaultHotkeys() {
        FunctionToKeyMap.Clear();
        _hotkeyConfig.FunctionToKeyMap = HotkeyConfig.CreateDefaultHotkeyConfig();
    }

    /// <summary>
    ///     清空热键
    /// </summary>
    public static void ClearKeyGestures() { FunctionToKeyMap.Clear(); }

    /// <summary>
    ///     处理按键事件
    /// </summary>
    /// <param name="e">按键事件参数</param>
    public static void HandleKeyDown(KeyEventArgs e) {
        if (!IsEnable)
            return;

        foreach (KeyValuePair<HotkeyFunction, List<SerializableKeyGesture>> kvp in
                 FunctionToKeyMap.Where(kvp => kvp.Value.Any(gesture => gesture.ToKeyGesture().Matches(e))))
            try {
                if (!_functionToActionMap.TryGetValue(kvp.Key, out Action? action))
                    continue;

                action.Invoke();
                e.Handled = true;

                return;
            } catch (Exception ex) {
                LoggerService.Error($"热键执行异常 [{kvp.Key}]: {ex.Message}\n{ex.StackTrace}");
            }
    }

    /// <summary>
    ///     获取功能的描述名称
    /// </summary>
    /// <param name="function">功能枚举</param>
    /// <returns>功能描述</returns>
    public static string GetFunctionDescription(HotkeyFunction function) {
        return function switch {
            HotkeyFunction.Previous         => "上一首",
            HotkeyFunction.Next             => "下一首",
            HotkeyFunction.TogglePlay       => "播放/暂停",
            HotkeyFunction.ToggleMute       => "静音切换",
            HotkeyFunction.SwitchPlayMode   => "播放模式切换",
            HotkeyFunction.VolumeUp         => "音量增加",
            HotkeyFunction.VolumeDown       => "音量减少",
            HotkeyFunction.Replay           => "刷新当前音乐",
            HotkeyFunction.ShowPlaylistInfo => "显示播放列表信息",
            HotkeyFunction.ShowAudioInfo    => "显示当前播放信息",
            HotkeyFunction.NextPage         => "页面前进",
            HotkeyFunction.PrevPage         => "页面后退",
            _                               => throw new IndexOutOfRangeException()
        };
    }

    /// <summary>
    ///     检查按键冲突
    /// </summary>
    /// <param name="function">要检查的功能</param>
    /// <param name="gesture">要检查的按键</param>
    /// <returns>冲突信息，如果没有冲突返回null</returns>
    public static string? CheckKeyConflict(HotkeyFunction function, KeyGesture gesture) {
        List<HotkeyFunction> conflictingFunctions = FunctionToKeyMap
                                                    .Where(kvp => kvp.Value.Any(g => g.ToKeyGesture().Equals(gesture)))
                                                    .Select(kvp => kvp.Key)
                                                    .ToList();

        return conflictingFunctions.Where(conflictingFunction => conflictingFunction != function)
                                   .Select(conflictingFunction =>
                                               $"按键 {gesture} 已被功能 {GetFunctionDescription(conflictingFunction)} 使用")
                                   .FirstOrDefault();
    }

    /// <summary>
    ///     获取所有按键冲突
    /// </summary>
    /// <returns>冲突信息列表</returns>
    public static List<string> GetAllKeyConflicts() {
        var usedGestures = new Dictionary<KeyGesture, List<HotkeyFunction>>();

        // 收集所有使用的按键
        foreach (KeyValuePair<HotkeyFunction, List<SerializableKeyGesture>> kvp in FunctionToKeyMap)
        foreach (KeyGesture keyGesture in kvp.Value.Select(gesture => gesture.ToKeyGesture())) {
            if (!usedGestures.TryGetValue(keyGesture, out List<HotkeyFunction>? functions)) {
                functions = [];
                usedGestures[keyGesture] = functions;
            }

            functions.Add(kvp.Key);
        }

        // 返回冲突信息
        return usedGestures.Where(kvp => kvp.Value.Count > 1)
                           .Select(kvp => $"按键 {kvp.Key} 被多个功能使用: {string.Join(
                               ", ",
                               kvp.Value.Select(GetFunctionDescription))}")
                           .ToList();
    }

    /// <summary>
    ///     验证配置的有效性
    /// </summary>
    /// <returns>验证结果</returns>
    public static (bool IsValid, List<string> Errors) ValidateConfiguration() {
        List<string> conflicts = GetAllKeyConflicts();

        return (conflicts.Count == 0, conflicts);
    }

    /// <summary>
    ///     退出服务，清理所有引用
    /// </summary>
    public static void ClearCache() {
        UnregisterAllFunctionActions();

        FunctionToKeyMap.Clear();
        _functionToActionMap.Clear();
    }
}