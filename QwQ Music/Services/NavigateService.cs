// Add Linq for potential future use or clarity

using System;
using System.Collections.Generic;

namespace QwQ_Music.Services;

public static class NavigateService
{
    static NavigateService()
    {
        _navigationHistory.Add(CurrentView);
    }

    // 导航历史记录
    private static readonly List<string> _navigationHistory = [];
    private static int currentHistoryIndex;
    private static readonly Dictionary<string, string[]?> _viewTree = new()
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

    // 视图索引，存储每个视图的 (父视图名称?, 在父视图子节点中的索引, 当前子视图索引)
    private static Dictionary<string, (string? Parent, int IndexInParent, int CurrentChildIndex)> ViewIndex { get; } =
        GenerateViewIndex();

    private static Dictionary<string, (string?, int, int)> GenerateViewIndex()
    {
        var result = new Dictionary<string, (string?, int, int)>
        {
            { "窗口", (null, -1, 0) }, // 添加根节点
        };

        // 使用队列进行广度优先遍历，确保父节点先被处理
        var queue = new Queue<string>(_viewTree.Keys);
        var processed = new HashSet<string> { "窗口" }; // 防止重复处理

        while (queue.Count > 0)
        {
            string parent = queue.Dequeue();
            if (!_viewTree.TryGetValue(parent, out string[]? children) || children == null)
                continue;

            for (int i = 0; i < children.Length; i++)
            {
                string child = children[i];
                if (!result.ContainsKey(child)) // 确保只添加一次
                {
                    result.Add(child, (parent, i, 0)); // 默认子视图索引为0
                    if (processed.Contains(child))
                        continue;

                    queue.Enqueue(child);
                    processed.Add(child);
                }
                // 如果 ViewTree 中存在更深层次的定义，确保它们也被加入队列
                else if (_viewTree.ContainsKey(child) && !processed.Contains(child))
                {
                    queue.Enqueue(child);
                    processed.Add(child);
                }
            }
        }
        return result;
    }

    // 存储每个导航容器（父视图）的导航触发事件
    public static Dictionary<string, Action<int>?> NavigateEvents { get; set; } = new();

    // 当前活动视图改变时的事件
    public static Action<string>? CurrentViewChanged { get; set; }

    // 当前最深层级的活动视图名称
    public static string CurrentView { get; private set; } = "主页"; // Default view

    // 标记是否为内部导航（前进/后退），避免重复记录历史
    private static bool isNavigatingInternally;

    // 前进/后退状态属性
    public static bool CanGoBack => currentHistoryIndex > 0;
    public static bool CanGoForward => currentHistoryIndex < _navigationHistory.Count - 1;

    /// <summary>
    /// 更新导航历史记录
    /// </summary>
    private static void UpdateNavigationHistory()
    {
        if (isNavigatingInternally)
            return;

        // 如果当前索引不是历史记录的末尾，说明执行了后退操作后又进行了新的导航，
        // 需要清除当前索引之后的所有“未来”历史记录。
        if (currentHistoryIndex < _navigationHistory.Count - 1)
        {
            _navigationHistory.RemoveRange(currentHistoryIndex + 1, _navigationHistory.Count - currentHistoryIndex - 1);
        }

        // 避免连续添加相同的历史记录项
        if (_navigationHistory.Count != 0 && _navigationHistory[currentHistoryIndex] == CurrentView)
            return;

        _navigationHistory.Add(CurrentView);
        currentHistoryIndex = _navigationHistory.Count - 1; // 更新索引到最新的历史记录
    }

    /// <summary>
    /// 导航事件回调，由具体的导航容器（如 NavigationViewModel）触发，用于更新导航服务的状态。
    /// </summary>
    /// <param name="viewName">触发导航的视图（导航容器）名称</param>
    /// <param name="targetChildIndex">导航到的目标子视图在其父容器中的索引</param>
    public static void NavigateEvent(string viewName, int targetChildIndex)
    {
        if (
            !_viewTree.TryGetValue(viewName, out string[]? children)
            || children == null
            || targetChildIndex < 0
            || targetChildIndex >= children.Length
        )
        {
            return; // 无效导航，可以添加日志或错误处理
        }

        string targetChildView = children[targetChildIndex];

        // 更新当前视图为导航目标的子视图
        CurrentView = targetChildView;

        // 更新 ViewIndex 中父视图的当前子视图索引
        if (ViewIndex.TryGetValue(viewName, out var indexInfo))
        {
            ViewIndex[viewName] = (indexInfo.Parent, indexInfo.IndexInParent, targetChildIndex);
        }

        // 如果导航到的子视图本身也是一个导航容器，并且它有自己的当前选中项，
        // 则递归（或迭代）更新 CurrentView 到最深层的活动视图。
        // 我们只需要更新 CurrentView 指向最深层的视图。
        string deepestView = targetChildView;
        while (
            _viewTree.TryGetValue(deepestView, out string[]? grandChildren)
            && grandChildren != null
            && ViewIndex.TryGetValue(deepestView, out var childIndexInfo)
            && childIndexInfo.CurrentChildIndex >= 0
            && childIndexInfo.CurrentChildIndex < grandChildren.Length
        )
        {
            deepestView = grandChildren[childIndexInfo.CurrentChildIndex];
        }
        CurrentView = deepestView; // 更新 CurrentView 为最深层活动视图

        UpdateNavigationHistory();

        // 触发 CurrentViewChanged 事件，通知UI或其他部分当前视图已更改
        CurrentViewChanged?.Invoke(CurrentView);
    }

    /// <summary>
    /// 导航到指定的视图名称。
    /// </summary>
    /// <param name="targetViewName">目标视图的名称</param>
    public static void NavigateTo(string targetViewName)
    {
        if (!ViewIndex.TryGetValue(targetViewName, out var targetIndexInfo))
            return; // 目标视图不存在

        string? currentParentName = targetIndexInfo.Parent;
        int childIndex = targetIndexInfo.IndexInParent;

        // 使用栈来存储从目标视图到根节点的路径上的导航事件调用参数
        var navigationStack = new Stack<(string ParentName, int ChildIndex)>();

        // 从目标视图向上遍历，直到找到根节点或已经处理过的父节点
        while (currentParentName != null && ViewIndex.TryGetValue(currentParentName, out var parentIndexInfo))
        {
            // 如果父视图的当前子视图不是通往目标视图的路径，则记录需要触发的导航
            if (parentIndexInfo.CurrentChildIndex != childIndex)
            {
                navigationStack.Push((currentParentName, childIndex));
            }
            // 继续向上查找
            childIndex = parentIndexInfo.IndexInParent;
            currentParentName = parentIndexInfo.Parent;
        }

        // 从最顶层的父视图开始，依次触发导航事件
        while (navigationStack.Count > 0)
        {
            (string parentName, int indexToSelect) = navigationStack.Pop();
            // 触发父视图的导航事件，但不添加到历史记录 (因为这是程序化导航)
            // 注意：NavigateEvents 的 Action<int> 参数是子视图的索引
            NavigateEvents.TryGetValue(parentName, out var navigateAction);
            navigateAction?.Invoke(indexToSelect);
            // NavigateEvent 会被调用，更新 CurrentView 和 ViewIndex[parentName] 的 CurrentChildIndex
        }

        // 最后确保 CurrentView 更新正确，并触发 Changed 事件
        // （NavigateEvent 内部会处理这个，但如果栈为空，需要手动确保）
        if (navigationStack.Count != 0 || CurrentView == targetViewName)
            return;

        // 如果目标视图本身就是根节点的直接子节点，且未发生变化，可能无需额外操作
        // 但如果目标视图有更深的子节点，需要确保 CurrentView 更新到最深层
        string deepestView = targetViewName;
        while (
            _viewTree.TryGetValue(deepestView, out string[]? grandChildren)
            && grandChildren != null
            && ViewIndex.TryGetValue(deepestView, out var childIndexInfo)
            && childIndexInfo.CurrentChildIndex >= 0
            && childIndexInfo.CurrentChildIndex < grandChildren.Length
        )
        {
            deepestView = grandChildren[childIndexInfo.CurrentChildIndex];
        }

        if (CurrentView == deepestView)
            return;

        CurrentView = deepestView;
        CurrentViewChanged?.Invoke(CurrentView);
        UpdateNavigationHistory(); // NavigateTo 应该更新历史记录
    }

    /// <summary>
    /// 导航后退
    /// </summary>
    /// <returns>是否成功后退</returns>
    public static bool GoBack()
    {
        if (!CanGoBack)
            return false;

        currentHistoryIndex--;
        string targetView = _navigationHistory[currentHistoryIndex];
        InternalNavigate(targetView); // 使用内部导航，不添加新历史记录
        return true;
    }

    /// <summary>
    /// 导航前进
    /// </summary>
    /// <returns>是否成功前进</returns>
    public static bool GoForward()
    {
        if (!CanGoForward)
            return false;

        currentHistoryIndex++;
        string targetView = _navigationHistory[currentHistoryIndex];
        InternalNavigate(targetView); // 使用内部导航，不添加新历史记录
        return true;
    }

    /// <summary>
    /// 内部导航方法，用于前进/后退，避免重复记录历史。
    /// </summary>
    /// <param name="targetViewName">目标视图名称</param>
    private static void InternalNavigate(string targetViewName)
    {
        isNavigatingInternally = true; // 标记开始内部导航
        try
        {
            NavigateTo(targetViewName); // 调用 NavigateTo 执行导航
        }
        finally
        {
            isNavigatingInternally = false; // 标记结束内部导航
        }
    }
}
