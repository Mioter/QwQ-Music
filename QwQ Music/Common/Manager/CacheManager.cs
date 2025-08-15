using System;
using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Media.Imaging;
using QwQ_Music.Common.Utilities;

namespace QwQ_Music.Common.Manager;

public static class CacheManager
{
    public static Bitmap NotExist { get; } = GetDefaultCover("没有图片哦");

    public static Bitmap Loading { get; } = GetDefaultCover("图片绘制中");

    public static Bitmap Damaged { get; } = GetDefaultCover("图片压坏了");

    public static Bitmap Default { get; } = GetDefaultCover("看我");

    public static WeakCache<string, Bitmap> ImageCache { get; } = new();

    /// <summary>
    ///     设置或更新图片到缓存
    /// </summary>
    public static void SetImage(string id, Bitmap bitmap)
    {
        ImageCache[id] = bitmap;
    }

    /// <summary>
    ///     通过图片Id删除图片
    /// </summary>
    public static void DeleteImage(string id)
    {
        ImageCache.Remove(id);
    }

    /// <summary>
    ///     通过图片Id集合批量删除图片
    /// </summary>
    public static void DeleteImages(IEnumerable<string> ids)
    {
        foreach (string id in ids)
        {
            ImageCache.Remove(id);
        }
    }

    /// <summary>
    ///     获取内置图片
    /// </summary>
    /// <returns></returns>
    /// <exception cref="FileNotFoundException">无法找到图片资源时抛出异常</exception>
    private static Bitmap GetDefaultCover(string imageFileName)
    {
        try
        {
            var assembly = App.CurrentAssembly;

            using var stream =
                assembly.GetManifestResourceStream($"QwQ_Music.Assets.EmbeddedRes.Images.{imageFileName}.webp")
             ?? throw new FileNotFoundException($"无法找到 {imageFileName}.webp 资源");

            return new Bitmap(stream);
        }
        catch (Exception)
        {
            // 如果资源加载失败，返回一个空位图
            var bitmap = new RenderTargetBitmap(new PixelSize(100, 100));

            return bitmap;
        }
    }
}
