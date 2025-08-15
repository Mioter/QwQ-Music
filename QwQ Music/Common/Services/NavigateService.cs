using System;
using System.Collections.Generic;

namespace QwQ_Music.Common.Services;

public static class NavigateService
{
    // 导航历史记录
    private static readonly List<string> _navigationHistory = [];
    private static int currentHistoryIndex;
    private static readonly Dictionary<string, string[]?> _viewTree = new()
    {
        {
            "窗口", ["主页", "分类", "其他", "设置"]
        },
        {
            "主页", null
        },
        {
            "分类", ["歌单", "专辑"]
        },
        {
            "其他", ["统计", "玩的", "关于"]
        },
        {
            "设置", ["界面", "播放", "歌词", "音效", "系统", "按键"]
        },
        {
            "歌单", ["全部歌单", "歌单详情"]
        },
        {
            "专辑", ["全部专辑", "专辑详情"]
        },
        {
            "音效", [
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

    // 标记是否为内部导航（前进/后退），避免重复记录历史
    private static bool isNavigatingInternally;

    // 标记是否为内部阶段导航，避免被记录历史
    private static bool isNavigatingMultistage;

    static NavigateService()
    {
        _navigationHistory.Add(CurrentView);
    }

    // 视图索引，存储每个视图的 (父视图名称, 在父视图子节点中的索引, 当前子视图索引)
    private static Dictionary<string, (string? Parent, int IndexInParent, int CurrentChildIndex)> ViewIndex { get; } =
        GenerateViewIndex();

    /// <summary>
    ///     存储每个导航容器（父视图）的导航触发事件
    /// </summary>
    public static Dictionary<string, Action<int>?> NavigateToEvents { get; set; } = new();

    /// <summary>
    ///     当导航到key对应的视图，则触发其事件
    /// </summary>
    public static Dictionary<string, Action> ComeToOneselfEvents { get; set; } = new();

    /// <summary>
    ///     当前活动视图改变时的事件，此事件用于通知订阅者当前视图名称
    /// </summary>
    public static Action<string>? CurrentViewChanged { get; set; }

    /// <summary>
    ///     当前最深层级的活动视图名称
    /// </summary>
    public static string CurrentView { get; private set; } = "主页"; // Default view

    // 前进/后退状态属性
    public static bool CanGoBack => currentHistoryIndex > 0;

    public static bool CanGoForward => currentHistoryIndex < _navigationHistory.Count - 1;

    private static Dictionary<string, (string?, int, int)> GenerateViewIndex()
    {
        var result = new Dictionary<string, (string?, int, int)>
        {
            ["窗口"] = (null, -1, 0), // 添加根节点
        };

        // 使用队列进行广度优先遍历，确保父节点先被处理
        var queue = new Queue<string>(_viewTree.Keys);

        var processed = new HashSet<string>
        {
            "窗口",
        }; // 防止重复处理

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

    /// <summary>
    ///     更新导航历史记录
    /// </summary>
    private static void UpdateNavigationHistory()
    {
        if (isNavigatingInternally || isNavigatingMultistage)
            return;

        // 如果当前索引不是历史记录的末尾，说明执行了后退操作后又进行了新的导航，
        // 需要清除当前索引之后的所有"未来"历史记录。
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
    ///     导航事件回调，由具体的导航容器（如 NavigationViewModel）触发，用于更新导航服务的状态。
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
        ComeToOneselfEvents.TryGetValue(CurrentView, out var navigateAction);
        navigateAction?.Invoke();
    }

    /// <summary>
    ///     导航到指定的视图名称。
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
            isNavigatingMultistage = navigationStack.Count > 1;

            (string parentName, int indexToSelect) = navigationStack.Pop();

            // 触发父视图的导航事件，但不添加到历史记录 (因为这是程序化导航)
            // 注意：NavigateToEvents 的 Action<int> 参数是子视图的索引
            NavigateToEvents.TryGetValue(parentName, out var action);
            action?.Invoke(indexToSelect);

            // NavigateEvent 会被调用，更新 CurrentView 和 ViewIndex[parentName] 的 CurrentChildIndex
        }
    }

    /// <summary>
    ///     导航后退
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
    ///     导航前进
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
    ///     内部导航方法，用于前进/后退，避免重复记录历史。
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

    /// <summary>
    ///     获取指定视图的父视图名称
    /// </summary>
    /// <param name="viewName">要查询的视图名称</param>
    /// <returns>父视图名称，如果不存在则返回 null</returns>
    public static string? GetParentView(string viewName)
    {
        return ViewIndex.TryGetValue(viewName, out var indexInfo) ? indexInfo.Parent : null;
    }

    /// <summary>
    ///     获取子视图在父视图中的索引
    /// </summary>
    /// <param name="parentViewName">父视图名称</param>
    /// <param name="childViewName">子视图名称</param>
    /// <returns>子视图在父视图中的索引，如果不存在则返回-1</returns>
    public static int GetChildViewIndex(string parentViewName, string childViewName)
    {
        if (!_viewTree.TryGetValue(parentViewName, out string[]? children) || children == null)
            return -1;

        return Array.IndexOf(children, childViewName);
    }

    /// <summary>
    ///     清理引用
    /// </summary>
    public static void ClearCache()
    {
        ComeToOneselfEvents.Clear();
        NavigateToEvents.Clear();
    }
}
