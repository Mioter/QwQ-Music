using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using QwQ_Music.Services;
using QwQ_Music.Utilities;

namespace QwQ_Music.Helper;

public static class ImageHelper
{
    /// <summary>
    /// 从Avalonia资源加载图片
    /// </summary>
    /// <param name="resourceUri"></param>
    /// <returns></returns>
    public static Bitmap LoadFromResource(Uri resourceUri)
    {
        return new Bitmap(AssetLoader.Open(resourceUri));
    }

    /// <summary>
    /// 从web加载图片
    /// </summary>
    /// <param name="url">图片直连</param>
    /// <returns></returns>
    public static async Task<Bitmap?> LoadFromWeb(Uri url)
    {
        using var httpClient = new HttpClient();
        try
        {
            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            byte[] data = await response.Content.ReadAsByteArrayAsync();
            return new Bitmap(new MemoryStream(data));
        }
        catch (HttpRequestException ex)
        {
            await LoggerService.ErrorAsync($"An error occurred while downloading image '{url}' : {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 从网络加载图片并压缩到指定大小以内
    /// </summary>
    /// <param name="url">图片URL</param>
    /// <param name="maxSizeInBytes">最大文件大小（字节）</param>
    /// <returns>压缩后的位图</returns>
    public static async Task<Bitmap?> LoadFromWebAndCompress(Uri url, long maxSizeInBytes)
    {
        using var httpClient = new HttpClient();
        try
        {
            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            byte[] data = await response.Content.ReadAsByteArrayAsync();

            // 如果原始数据已经小于目标大小，直接返回
            if (data.Length <= maxSizeInBytes)
            {
                return new Bitmap(new MemoryStream(data));
            }

            // 压缩图片
            var originalBitmap = new Bitmap(new MemoryStream(data));
            return ImageCompression.CompressBitmapAsync(originalBitmap, maxSizeInBytes);
        }
        catch (HttpRequestException ex)
        {
            await LoggerService.ErrorAsync($"An error occurred while downloading image '{url}' : {ex.Message}");
            return null;
        }
    }
}
