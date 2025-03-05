using System;
using System.Collections.Generic;

namespace QwQ_Music.Models;

public static partial class LanguageModel
{
    public enum Language
    {
        en_US,
        zh_CN,
    }

    public static Dictionary<string, string> Lang = new()
    {
        ["MusicName"] = "音乐",
        ["ClassificationName"] = "分类",
        ["StatisticsName"] = "统计",
        ["SettingsName"] = "设置",
        ["LyricConfigName"] = "歌词",
        ["OffsetName"] = "偏移",
        ["IsEnabledName"] = "启用",
        ["IsDoubleLineName"] = "双行模式",
        ["IsDualLangName"] = "显示翻译",
        ["IsVerticalName"] = "纵向模式",
        ["PositionXName"] = "横向顶点坐标",
        ["PositionYName"] = "纵向顶点坐标",
        ["WidthName"] = "宽度",
        ["HeightName"] = "高度",
        ["MaximizeName"] = "最大化",
        ["ResetName"] = "恢复默认值",
        ["LyricMainTopColorName"] = "主要歌词顶部",
        ["LyricMainBottomColorName"] = "主要歌词底部",
        ["LyricMainBorderColorName"] = "主要歌词描边",
        ["LyricAltTopColorName"] = "备选歌词顶部",
        ["LyricAltBottomColorName"] = "备选歌词底部",
        ["LyricAltBorderColorName"] = "备选歌词描边",
        ["Loading..."] = "加载中...",
    };

    private class UnfinishedFunctionException(string msg) : InvalidOperationException(msg);

    public static void LoadLanguage(Language language)
    {
        throw new UnfinishedFunctionException(nameof(LoadLanguage));
    }
}
