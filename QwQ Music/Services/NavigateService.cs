using System;
using System.Collections.Generic;

namespace QwQ_Music.Services;

public static class NavigateService
{
    private static readonly Dictionary<string, string[]?> ViewTree = new()
    {
        { "窗口", ["主页", "分类", "统计", "设置"] },
        { "主页", null },
        { "分类", null },
        { "统计", null },
        { "设置", ["界面", "播放", "歌词", "音效"] },
        {
            "音效",
            [
                "立体增强",
                "全景效果",
                "空间效果",
                "环绕效果",
                "混响效果",
                "延迟效果",
                "失真效果",
                "颤音效果",
                "压缩器",
                "均衡器",
                "噪音减少",
            ]
        },
    };

    // 自动根据ViewTree生成ViewIndex
    private static Dictionary<string, (string?, int, int)> ViewIndex { get; } = GenerateViewIndex();

    private static Dictionary<string, (string?, int, int)> GenerateViewIndex()
    {
        var result = new Dictionary<string, (string?, int, int)>
        {
            { "窗口", (null, -1, 0) }, // 添加根节点
        };

        // 递归处理所有节点
        foreach ((string parent, string[]? children) in ViewTree)
        {
            if (children == null)
                continue;

            for (int i = 0; i < children.Length; i++)
            {
                string child = children[i];
                // 如果子节点还没有添加到结果中
                if (!result.ContainsKey(child))
                {
                    result.Add(child, (parent, i, 0));
                }
            }
        }

        return result;
    }

    public static Dictionary<string, Action<int>?> NavigateEvents { get; set; } = new();

    public static Action<string>? CurrentViewChanged { get; set; }

    public static string? CurrentView { get; private set; }

    public static void NavigateTo(string view, int toIndex)
    {
        CurrentView = ViewTree[view]?[toIndex];

        if (ViewIndex.TryGetValue(view, out var index))
        {
            ViewIndex[view] = (index.Item1, index.Item2, toIndex);

            if (
                ViewTree.TryGetValue(view, out string[]? child)
                && child?[toIndex] is { } childView
                && ViewIndex[childView].Item3 is var childIndex and > 0
            )
            {
                NavigateTo(childView, childIndex);
            }
        }

        if (CurrentView != null && CurrentViewChanged != null)
        {
            CurrentViewChanged.Invoke(CurrentView);
        }
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
