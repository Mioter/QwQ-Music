using Avalonia.Media.Imaging;
using Avalonia.Platform;
using QwQ_Music.Common.Services;
using QwQ_Music.Common.Utilities;

namespace QwQ_Music.Common.Helpers;

public static class ImageHelper {
    private static readonly HttpClient _httpClient = new();

    /// <summary>
    ///     从Avalonia资源加载图片
    /// </summary>
    /// <param name="resourceUri"></param>
    /// <returns></returns>
    public static Bitmap LoadFromResource(Uri resourceUri) { return new Bitmap(AssetLoader.Open(resourceUri)); }

    /// <summary>
    ///     从web加载图片
    /// </summary>
    /// <param name="url">图片直连</param>
    /// <returns></returns>
    public static async ValueTask<Bitmap?> LoadFromWeb(Uri url) {
        try {
            HttpResponseMessage response = await _httpClient.GetAsync(url).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            byte[] data = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

            return new Bitmap(new MemoryStream(data));
        } catch (HttpRequestException ex) {
            await LoggerService.ErrorAsync($"An error occurred while downloading image '{url}' : {ex.Message}")
                               .ConfigureAwait(false);

            return null;
        }
    }

    /// <summary>
    ///     从网络加载图片并压缩到指定大小以内
    /// </summary>
    /// <param name="url">图片URL</param>
    /// <param name="maxSizeInBytes">最大文件大小（字节）</param>
    /// <returns>压缩后的位图</returns>
    public static async ValueTask<Bitmap?> LoadFromWebAndCompress(Uri url, long maxSizeInBytes) {
        try {
            HttpResponseMessage response = await _httpClient.GetAsync(url).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            byte[] data = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

            // 压缩图片
            var originalBitmap = new Bitmap(new MemoryStream(data));

            return await BitmapCompression.CompressBitmapAsync(originalBitmap, maxSizeInBytes).ConfigureAwait(false);
        } catch (HttpRequestException ex) {
            await LoggerService.ErrorAsync($"An error occurred while downloading image '{url}' : {ex.Message}")
                               .ConfigureAwait(false);

            return null;
        }
    }

    /// <summary>
    ///     从网络加载图片并压缩到指定大小以内
    /// </summary>
    /// <param name="url">图片URL</param>
    /// <param name="width">最大尺寸（<see cref="width" /> * <see cref="width" />）</param>
    /// <returns>压缩后的位图</returns>
    public static async ValueTask<Bitmap?> LoadFromWebAndDecodeToWidthAsync(Uri url, int width = 128) {
        try {
            HttpResponseMessage response = await _httpClient.GetAsync(url).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            byte[] data = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

            // 压缩图片
            return Bitmap.DecodeToWidth(new MemoryStream(data), width);
        } catch (HttpRequestException ex) {
            await LoggerService.ErrorAsync($"An error occurred while downloading image '{url}' : {ex.Message}")
                               .ConfigureAwait(false);

            return null;
        }
    }

    /// <summary>
    ///     从文件系统中加载位图。
    /// </summary>
    /// <param name="coverPath">图片路径。</param>
    /// <param name="size">缩放后宽度。如果设置为-1则不缩放</param>
    /// <returns>所需的位图。</returns>
    public static async ValueTask<Bitmap?> LoadFromFileAsync(string coverPath, int size = -1) {
        // 获取文件流

        if (!File.Exists(coverPath)) {
            await LoggerService.WarningAsync($"无法打开{coverPath}: 文件不存在").ConfigureAwait(false);
            return null;
        }

        try {
            await using var fs = new FileStream(coverPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (size == -1) {
                await LoggerService.InfoAsync($"加载了{coverPath}的原始图像。").ConfigureAwait(false);
                return new Bitmap(fs);
            }

            await LoggerService.InfoAsync($"加载{coverPath}并裁剪中心区域最大{size}px*{size}px。").ConfigureAwait(false);
            return Bitmap.DecodeToWidth(fs, size);
        } catch (Exception ex) {
            await LoggerService.ErrorAsync($"加载图片错误({ex.GetType()}): {coverPath}\n" + $"{ex.Message}\n{ex.StackTrace}")
                               .ConfigureAwait(false);
            return null;
        }
    }


    /// <summary>
    ///     从文件系统中加载位图。
    /// </summary>
    /// <param name="stream">图片流。</param>
    /// <param name="name">图片名称</param>
    /// <param name="size">缩放后宽度。如果设置为-1则不缩放</param>
    /// <returns>所需的位图。</returns>
    public static async ValueTask<Bitmap?> LoadFromMemoryAsync(Stream stream, string name, int size = -1) {
        try {
            if (size == -1) {
                var result = new Bitmap(stream);
                await LoggerService.InfoAsync($"加载了[{name}]的原始图像。").ConfigureAwait(false);
                return result;
            }

            Bitmap resized = Bitmap.DecodeToWidth(stream, size);
            await LoggerService.InfoAsync($"成功加载图片流[{name}]并缩放到宽度{size}px。").ConfigureAwait(false);
            return resized;
        } catch (Exception ex) {
            await LoggerService.ErrorAsync($"加载图片流[{name}]错误", ex).ConfigureAwait(false);
            return null;
        } finally {
            await stream.DisposeAsync().ConfigureAwait(false);
        }
    }
}