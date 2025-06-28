using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace QwQ_Music.Services;

/// <summary>
/// 网易云音乐专辑爬虫服务 - 简化版本
/// 直接使用网易云音乐 API 获取专辑信息
/// </summary>
public class NetEaseAlbumCrawler : IDisposable
{
    private readonly NetEaseAlbumCrawlerOptions _options;
    private bool _disposed;

    // API 端点
    private const string SEARCH_API_URL =
        "https://music.163.com/api/search/get/web?csrf_token=&s={0}&type=10&limit=10&offset=0";
    private const string ALBUM_DETAIL_API_URL = "https://music.163.com/api/v1/album/{0}";

    // 使用源生成的 JsonSerializerContext，支持 AOT 编译
    private static readonly AlbumSearchResultJsonContext _jsonContext = new();
    private static readonly AlbumDetailResultJsonContext _detailJsonContext = new();

    /// <summary>
    /// 构造函数
    /// </summary>
    public NetEaseAlbumCrawler(NetEaseAlbumCrawlerOptions? options = null)
    {
        _options = options ?? new NetEaseAlbumCrawlerOptions();
    }

    /// <summary>
    /// 通过专辑名称获取专辑ID（可选艺人名）
    /// </summary>
    public async Task<string?> GetAlbumIdByNameAsync(
        string albumName,
        string? artistName = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(albumName);
        ThrowIfDisposed();

        // 如果有艺术家名，优先尝试组合搜索
        if (!string.IsNullOrWhiteSpace(artistName))
        {
            string? combinedSearchResult = await SearchAlbumAsync(
                $"{albumName}-{artistName}",
                albumName,
                artistName,
                cancellationToken
            );
            if (!string.IsNullOrEmpty(combinedSearchResult))
                return combinedSearchResult;
        }

        // 组合搜索失败或没有艺术家名，使用专辑名单独搜索
        return await SearchAlbumAsync(albumName, albumName, artistName, cancellationToken);
    }

    /// <summary>
    /// 通过专辑ID获取专辑详情
    /// </summary>
    public async Task<AlbumDetail> GetAlbumDetailAsync(string albumId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(albumId);
        ThrowIfDisposed();

        string url = string.Format(ALBUM_DETAIL_API_URL, albumId);
        string json = await SendRequestAsync(url, cancellationToken);
        return ParseAlbumDetailFromApi(json);
    }

    /// <summary>
    /// 通过专辑名称直接获取专辑详情
    /// </summary>
    public async Task<AlbumDetail> GetAlbumDetailByNameAsync(
        string albumName,
        string? artistName = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(albumName);
        ThrowIfDisposed();

        try
        {
            string? albumId = await GetAlbumIdByNameAsync(albumName, artistName, cancellationToken);
            return albumId == null
                ? new AlbumDetail { Description = _options.NotFoundMessage }
                : await GetAlbumDetailAsync(albumId, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return new AlbumDetail { Description = "操作已取消" };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new AlbumDetail { Description = $"网络请求失败: {ex.Message}" };
        }
    }

    /// <summary>
    /// 发送HTTP请求并返回响应内容
    /// </summary>
    private async Task<string> SendRequestAsync(string url, CancellationToken cancellationToken = default)
    {
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(_options.RequestTimeoutSeconds);
        httpClient.DefaultRequestHeaders.Add("User-Agent", _options.UserAgent);
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json, text/plain, */*");
        httpClient.DefaultRequestHeaders.Add("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8");
        httpClient.DefaultRequestHeaders.Add("Referer", "https://music.163.com/");
        httpClient.DefaultRequestHeaders.Add("Origin", "https://music.163.com");

        return await httpClient.GetStringAsync(url, cancellationToken);
    }

    /// <summary>
    /// 搜索专辑
    /// </summary>
    private async Task<string?> SearchAlbumAsync(
        string searchTerm,
        string albumName,
        string? artistName,
        CancellationToken cancellationToken
    )
    {
        string url = string.Format(SEARCH_API_URL, HttpUtility.UrlEncode(searchTerm));
        string json = await SendRequestAsync(url, cancellationToken);
        return ParseSearchResults(json, albumName, artistName);
    }

    /// <summary>
    /// 解析搜索结果，返回专辑idStr
    /// </summary>
    private static string? ParseSearchResults(string json, string albumName, string? artistName)
    {
        try
        {
            var searchResult = JsonSerializer.Deserialize(json, _jsonContext.AlbumSearchResult);

            if (searchResult?.Result?.Albums == null || searchResult.Result.Albums.Count == 0)
                return null;

            var albums = searchResult.Result.Albums;

            // 如果没有艺术家名，返回第一个结果
            if (string.IsNullOrWhiteSpace(artistName))
                return albums[0].IdStr;

            // 有艺术家名时，按优先级查找匹配结果
            return FindBestMatch(albums, albumName, artistName)?.IdStr ?? albums[0].IdStr;
        }
        catch (JsonException ex)
        {
            throw new NetEaseAlbumCrawlerException($"解析搜索结果失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 查找最佳匹配的专辑
    /// </summary>
    private static AlbumItem? FindBestMatch(List<AlbumItem> albums, string albumName, string artistName)
    {
        // 1. 优先查找完全匹配的结果
        var exactMatch = albums.FirstOrDefault(album =>
            IsValidAlbum(album) && IsExactMatch(album.Name!, albumName) && IsExactMatch(album.Artist!.Name!, artistName)
        );

        if (exactMatch != null)
            return exactMatch;

        // 2. 查找模糊匹配的结果
        var fuzzyMatch = albums.FirstOrDefault(album =>
            IsValidAlbum(album) && IsFuzzyMatch(album.Name!, albumName) && IsFuzzyMatch(album.Artist!.Name!, artistName)
        );

        return fuzzyMatch;
    }

    /// <summary>
    /// 验证专辑数据是否有效
    /// </summary>
    private static bool IsValidAlbum(AlbumItem album) =>
        !string.IsNullOrEmpty(album.Name) && album.Artist != null && !string.IsNullOrEmpty(album.Artist.Name);

    /// <summary>
    /// 精确匹配
    /// </summary>
    private static bool IsExactMatch(string found, string search) =>
        string.Equals(NormalizeString(found), NormalizeString(search), StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 模糊匹配
    /// </summary>
    private static bool IsFuzzyMatch(string found, string search)
    {
        string normalizedFound = NormalizeString(found);
        string normalizedSearch = NormalizeString(search);

        return normalizedFound.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)
            || normalizedSearch.Contains(normalizedFound, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 从 API 响应解析专辑详情
    /// </summary>
    private static AlbumDetail ParseAlbumDetailFromApi(string json)
    {
        try
        {
            var detailResult = JsonSerializer.Deserialize(json, _detailJsonContext.AlbumDetailResult);

            if (detailResult?.Album == null)
            {
                return new AlbumDetail { Description = "未找到专辑信息" };
            }

            var album = detailResult.Album;

            return new AlbumDetail
            {
                Description = !string.IsNullOrWhiteSpace(album.Description) ? album.Description : "暂无专辑简介",
                PublishTime = FormatPublishTime(album.PublishTime),
                Company = !string.IsNullOrWhiteSpace(album.Company) ? album.Company : string.Empty,
            };
        }
        catch (JsonException ex)
        {
            throw new NetEaseAlbumCrawlerException($"解析专辑详情失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 格式化发布时间
    /// </summary>
    private static string FormatPublishTime(long publishTime) =>
        publishTime > 0 ? DateTimeOffset.FromUnixTimeMilliseconds(publishTime).ToString("yyyy-MM-dd") : string.Empty;

    /// <summary>
    /// 字符串归一化
    /// </summary>
    private static string NormalizeString(string input) =>
        input.Replace(" ", "").Replace("-", "").Replace("_", "").Replace(".", "");

    /// <summary>
    /// 检查是否已释放
    /// </summary>
    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed || !disposing)
            return;

        _disposed = true;
    }

    ~NetEaseAlbumCrawler() => Dispose(false);
}

/// <summary>
/// 网易云专辑爬虫配置选项
/// </summary>
public class NetEaseAlbumCrawlerOptions
{
    /// <summary>
    /// 请求超时时间（秒）
    /// </summary>
    public int RequestTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// User-Agent
    /// </summary>
    public string UserAgent { get; set; } =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36";

    /// <summary>
    /// 未找到专辑时的消息
    /// </summary>
    public string NotFoundMessage { get; set; } = "未找到专辑";
}

/// <summary>
/// 网易云专辑爬虫异常
/// </summary>
public class NetEaseAlbumCrawlerException : Exception
{
    public NetEaseAlbumCrawlerException(string message)
        : base(message) { }

    public NetEaseAlbumCrawlerException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>
/// 专辑搜索结果
/// </summary>
[JsonSerializable(typeof(AlbumSearchResult))]
public partial class AlbumSearchResultJsonContext : JsonSerializerContext { }

/// <summary>
/// 专辑详情 API 相关的 JSON 模型
/// </summary>
[JsonSerializable(typeof(AlbumDetailResult))]
public partial class AlbumDetailResultJsonContext : JsonSerializerContext { }

public class AlbumSearchResult
{
    [JsonPropertyName("result")]
    public ResultData? Result { get; set; }

    [JsonPropertyName("code")]
    public int Code { get; set; }
}

public class ResultData
{
    [JsonPropertyName("albums")]
    public List<AlbumItem>? Albums { get; set; }

    [JsonPropertyName("albumCount")]
    public int AlbumCount { get; set; }
}

public class AlbumItem
{
    [JsonPropertyName("idStr")]
    public string? IdStr { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("artist")]
    public ArtistItem? Artist { get; set; }

    [JsonPropertyName("artists")]
    public List<ArtistItem>? Artists { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("company")]
    public string? Company { get; set; }

    [JsonPropertyName("publishTime")]
    public long PublishTime { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("picUrl")]
    public string? PicUrl { get; set; }
}

public class ArtistItem
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("picUrl")]
    public string? PicUrl { get; set; }

    [JsonPropertyName("alias")]
    public List<string>? Alias { get; set; }
}

public class AlbumDetailResult
{
    [JsonPropertyName("album")]
    public AlbumDetailItem? Album { get; set; }

    [JsonPropertyName("code")]
    public int Code { get; set; }
}

public class AlbumDetailItem
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("company")]
    public string? Company { get; set; }

    [JsonPropertyName("publishTime")]
    public long PublishTime { get; set; }

    [JsonPropertyName("picUrl")]
    public string? PicUrl { get; set; }

    [JsonPropertyName("artist")]
    public ArtistItem? Artist { get; set; }

    [JsonPropertyName("artists")]
    public List<ArtistItem>? Artists { get; set; }
}

public class AlbumDetail
{
    public string Description { get; set; } = string.Empty;
    public string PublishTime { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
}
