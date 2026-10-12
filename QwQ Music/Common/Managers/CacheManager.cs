using System.Reflection;
using Avalonia;
using Avalonia.Media.Imaging;
using QwQ_Music.Common.Helpers;
using QwQ_Music.Common.Interfaces;
using QwQ_Music.Common.Services;
using QwQ_Music.Common.Utilities;
using QwQ_Music.Models.Enums;

namespace QwQ_Music.Common.Managers;

public static class CacheManager {
    public static readonly Dictionary<AudioQualityLevel, Bitmap> AudioQualityLevelLogo = new() {
        [AudioQualityLevel.PQ] = GetBuiltInImage("PQ.png"),
        [AudioQualityLevel.HQ] = GetBuiltInImage("HQ.png"),
        [AudioQualityLevel.SQ] = GetBuiltInImage("SQ.png"),
        [AudioQualityLevel.HR] = GetBuiltInImage("HR.png")
    };

    // public static Bitmap NotExist {
    //     get {
    //         try {
    //             _ = field.PixelSize;
    //         } catch (NullReferenceException) {
    //             field = GetBuiltInImage("没有图片哦.webp");
    //         }
    //
    //         return field;
    //     }
    // } = GetBuiltInImage("没有图片哦.webp");

    public static Bitmap Loading {
        get {
            try {
                _ = field.PixelSize;
            } catch (Exception ex) when (ex is NullReferenceException or ObjectDisposedException) {
                field = GetBuiltInImage("图片绘制中.webp");
                LoggerService.Warning("Loading 图片资源意外释放，正在重新加载...");
            }

            return field;
        }
    } = GetBuiltInImage("图片绘制中.webp");

    public static Bitmap Damaged {
        get {
            try {
                _ = field.PixelSize;
            } catch (Exception ex) when (ex is NullReferenceException or ObjectDisposedException) {
                field = GetBuiltInImage("图片压坏了.webp");
                LoggerService.Warning("Damaged 图片资源意外释放，正在重新加载...");
            }

            return field;
        }
    } = GetBuiltInImage("图片压坏了.webp");

    public static Bitmap NotExist {
        get {
            try {
                _ = field.PixelSize;
            } catch (Exception ex) when (ex is NullReferenceException or ObjectDisposedException) {
                field = GetBuiltInImage("看我.webp");
                LoggerService.Warning("NotExist 图片资源意外释放，正在重新加载...");
            }

            return field;
        }
    } = GetBuiltInImage("看我.webp");

    public static WeakCache<string, Bitmap> ImageCache { get; } = new();

    /// <summary>
    ///     设置或更新图片到缓存
    /// </summary>
    public static void SetImage<T>(T? id, string idType, Bitmap bitmap) where T : notnull {
        if (id is null || bitmap == Loading)
            return;
        ImageCache[$"{idType}:{id}"] = bitmap;
        LoggerService.Debug($"[{idType}:{id}]缩略图缓存已添加");
    }

    /// <summary>
    ///     通过图片Id删除图片
    /// </summary>
    public static void DeleteImage<T>(T id) where T : notnull {
        if (id.ToString() is not { } key)
            return;
        ImageCache.Remove(key);
    }

    /// <summary>
    ///     通过图片Id集合批量删除图片
    /// </summary>
    public static void DeleteImages<T>(IEnumerable<T> ids) where T : notnull {
        foreach (string? id in ids.Select(id => id.ToString())) {
            if (id is null)
                continue;
            ImageCache.Remove(id);
        }
    }

    /// <summary>
    ///     获取内置图片
    /// </summary>
    /// <returns></returns>
    /// <exception cref="FileNotFoundException">无法找到图片资源时抛出异常</exception>
    public static Bitmap GetBuiltInImage(string imageFileName) {
        try {
            Assembly assembly = App.CurrentAssembly;

            using Stream stream =
                assembly.GetManifestResourceStream($"QwQ_Music.Assets.EmbeddedRes.Images.{imageFileName}") ??
                throw new FileNotFoundException($"无法找到 {imageFileName} 资源");

            return new Bitmap(stream);
        } catch (Exception) {
            // 如果资源加载失败，返回一个空位图
            var bitmap = new RenderTargetBitmap(new PixelSize(1, 1));

            return bitmap;
        }
    }

    private static Bitmap? TryLoadFromMemory(string? id, string idType, string cacheType, string? alterId) {
        if (id is null)
            return null;

        // 尝试从缓存获取图片
        if (!ImageCache.TryGetValue($"{idType}:{id}", out Bitmap? image) ||
            image == null ||
            // image == NotExist ||
            image == Loading)
            return null;

        try {
            _ = image.PixelSize;
            LoggerService.Debug($"缓存命中: 已加载{idType}[{alterId ?? id}]的{cacheType}");
            return image;
        } catch (Exception ex) when (ex is NullReferenceException or ObjectDisposedException) {
            ImageCache.Remove($"{idType}:{id}");
            return null;
        }
    }

    public static async ValueTask<Bitmap> TryLoadFromWebAsync(
        string? id,
        string idType,
        string cacheType,
        Uri? uri,
        Action? callIfExist,
        string? alterId = null,
        int maxWidth = 128) {
        if (id is null || uri is null)
            return NotExist;

        if (TryLoadFromMemory(id, idType, cacheType, alterId) is { } cache)
            return cache;

        return await Task.Run(async Task<Bitmap>? () => {
                             try {
                                 await LoggerService.InfoAsync($"尝试加载{idType}[{alterId ?? id}]的{cacheType}...")
                                                    .ConfigureAwait(false);

                                 Bitmap? bitmap =
                                     await ImageHelper.LoadFromWebAndDecodeToWidthAsync(uri, maxWidth)
                                                      .ConfigureAwait(false);

                                 if (bitmap is null) {
                                     SetImage(id, idType, NotExist);
                                     await LoggerService.InfoAsync($"尝试加载{idType}({alterId ?? id})的{cacheType}，但不存在。")
                                                        .ConfigureAwait(false);
                                     return NotExist;
                                 }

                                 SetImage(id, idType, bitmap);
                                 await LoggerService.InfoAsync($"加载{idType}({alterId ?? id})的{cacheType}成功")
                                                    .ConfigureAwait(false);
                                 callIfExist?.Invoke();
                                 return bitmap;
                             } catch (Exception e) {
                                 await LoggerService.ErrorAsync($"加载{idType}({alterId ?? id})的{cacheType}时发生错误: {e}")
                                                    .ConfigureAwait(false);
                                 return Damaged;
                             }
                         })
                         .ConfigureAwait(false);
    }

    public static Bitmap TryLoadThumbnail<TPrimaryKey>(
        TPrimaryKey? id,
        string idType,
        string cacheType,
        IAsyncReadonlyDatabaseRepository<TPrimaryKey, Bitmap?> provider,
        Action? callIfExist,
        string? alterId = null) where TPrimaryKey : notnull {
        LoggerService.Debug($"尝试获取{idType}[{alterId ?? id?.ToString()}]的{cacheType}");

        if (id is null)
            return NotExist;

        if (TryLoadFromMemory(id.ToString(), idType, cacheType, alterId) is { } cache)
            return cache;
        _ = Task.Run(async Task? () => {
            try {
                await LoggerService.InfoAsync($"尝试加载{idType}[{alterId ?? id.ToString()}]的{cacheType}...")
                                   .ConfigureAwait(false);

                Bitmap? bitmap = await provider.SingleAsync(id).ConfigureAwait(false);

                if (bitmap is null) {
                    SetImage(id, idType, NotExist);
                    await LoggerService.InfoAsync($"尝试加载{idType}[{alterId ?? id.ToString()}]的{cacheType}，但不存在。")
                                       .ConfigureAwait(false);
                    return;
                }

                if (!ConfigManager.UiConfig.VisualConfig.AllowNonSquareCover &&
                    Math.Abs(bitmap.Size.AspectRatio - 1) > 1e-5)
                    BitmapCropper.Crop(ref bitmap, 1.0);
                SetImage(id, idType, bitmap);
                await LoggerService.InfoAsync($"加载{idType}[{alterId ?? id.ToString()}]的{cacheType}成功")
                                   .ConfigureAwait(false);
            } catch (Exception ex) {
                await LoggerService.ErrorAsync($"加载{idType}({alterId ?? id.ToString()})的{cacheType}时发生错误", ex)
                                   .ConfigureAwait(false);

                SetImage(id, idType, Damaged);
            } finally {
                callIfExist?.Invoke();
            }
        }).ContinueWith(LoggerService.HandleException).ConfigureAwait(false);
        return Loading;
    }

    /// <summary>
    ///     清理引用
    /// </summary>
    public static void Dispose() {
        // Default.Dispose();
        Loading.Dispose();
        NotExist.Dispose();
        Damaged.Dispose();

        foreach (Bitmap bitmap in AudioQualityLevelLogo.Values)
            bitmap.Dispose();
        ImageCache.Dispose();
    }

    public static bool IsValid(Bitmap image) {
        try {
            _ = image.PixelSize;
            return image != Damaged &&
                   image != NotExist &&
                   // image != Default && 
                   image != Loading;
        } catch (Exception ex) when (ex is NullReferenceException or ObjectDisposedException) {
            return false;
        }
    }
}