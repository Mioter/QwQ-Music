using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;

namespace QwQ_Music.Common.Services;

/// <summary>
///     访问 Avalonia 应用程序资源
/// </summary>
public static class ResourceAccessor
{
    /// <summary>
    ///     通过键获取指定类型的资源
    /// </summary>
    /// <typeparam name="T">要获取的资源类型</typeparam>
    /// <param name="key">资源的键</param>
    /// <returns>如果找到并且类型正确, 则返回资源; 否则, 返回该类型的默认值。</returns>
    public static T? Get<T>(object key)
    {
        if (
            Application.Current != null
         && Application.Current.TryGetResource(key, out object? resource)
         && resource is T typedResource
        )
        {
            return typedResource;
        }

        return default;
    }

    /// <summary>
    ///     使用指定的键添加新资源
    /// </summary>
    /// <param name="key">新资源的键</param>
    /// <param name="resource">要添加的资源</param>
    /// <returns>如果成功添加资源, 则返回 true; 否则返回 false。</returns>
    public static bool Add(object key, object resource)
    {
        return Application.Current != null && Application.Current.Resources.TryAdd(key, resource);
    }

    /// <summary>
    ///     替换或添加指定键的资源
    /// </summary>
    /// <param name="key">要替换或添加的资源的键</param>
    /// <param name="resource">新资源</param>
    public static bool Set(object key, object resource)
    {
        if (Application.Current == null)
            return false;

        Application.Current.Resources[key] = resource;

        return true;
    }

    /// <summary>
    ///     重命名资源键
    /// </summary>
    /// <param name="oldKey">资源的当前键</param>
    /// <param name="newKey">资源的新键</param>
    /// <returns>如果键成功重命名, 则返回 true; 否则返回 false。</returns>
    public static bool RenameKey(object oldKey, object newKey)
    {
        if (oldKey.Equals(newKey) || Application.Current == null)
        {
            return false;
        }

        if (Application.Current.Resources.ContainsKey(newKey))
        {
            // 新键已存在, 无法重命名
            return false;
        }

        if (!Application.Current.Resources.Remove(oldKey, out object? resource))
            return false;

        Application.Current.Resources.Add(newKey, resource);

        return true;
    }

    /// <summary>
    ///     通过其键删除资源
    /// </summary>
    /// <param name="key">要删除的资源的键</param>
    /// <returns>如果成功删除了资源, 则返回 true; 否则返回 false。</returns>
    public static bool Remove(object key)
    {
        return Application.Current != null && Application.Current.Resources.Remove(key);
    }
}
