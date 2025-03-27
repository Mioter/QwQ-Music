using System;
using System.Collections.Generic;

namespace QwQ_Music.Services;

public static class NavigateService
{
    private static Dictionary<string, (string?, int)> ViewIndex { get; set; } =
        new()
        {
            { "窗口", (null, -1) },
            { "主页", ("窗口", 0) },
            { "分类", ("窗口", 1) },
            { "统计", ("窗口", 2) },
            { "设置", ("窗口", 3) },
            { "歌词", ("设置", 0) },
            { "音效", ("设置", 1) },
            { "输出", ("设置", 2) },
            { "立体增强", ("音效", 0) },
            { "空间效果", ("音效", 1) },
            { "环绕效果", ("音效", 1) },
            { "混响效果", ("音效", 1) },
            { "延迟效果", ("音效", 1) },
            { "失真效果", ("音效", 1) },
            { "颤音效果", ("音效", 1) },
            { "压缩器", ("音效", 1) },
            { "均衡器", ("音效", 1) },
            { "淡入淡出", ("音效", 1) },
            { "回调增益", ("音效", 1) },
            { "噪音减少", ("音效", 1) },
        };

    private static readonly Dictionary<string, string[]?> ViewTree = new()
    {
        { "窗口", ["主页", "分类", "统计", "设置"] },
        { "主页", null },
        { "分类", null },
        { "统计", null },
        { "设置", ["歌词", "音效", "输出"] },
        {
            "音效",
            [
                "立体增强",
                "空间效果",
                "环绕效果",
                "混响效果",
                "延迟效果",
                "合唱效果",
                "失真效果",
                "颤音效果",
                "压缩器",
                "均衡器",
                "淡入淡出",
                "回调增益",
                "噪音减少",
            ]
        },
    };

    public static Dictionary<string, Action<int>?> NavigateEvents { get; set; } = new();

    public static string? CurrentView { get; private set; }

    public static void NavigateTo(string view, int toIndex)
    {
        CurrentView = ViewTree[view]?[toIndex];
    }

    public static void NavigateEvent(string eventName)
    {
        ViewIndex.TryGetValue(eventName, out var index);

        if (index.Item1 == null)
            return;

        if (index.Item2 >= 0 && CurrentView != index.Item1)
        {
            NavigateEvent(index.Item1);
        }

        NavigateEvents[index.Item1]?.Invoke(index.Item2);
    }
}
