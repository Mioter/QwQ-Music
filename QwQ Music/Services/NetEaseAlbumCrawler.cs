using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Io;

namespace QwQ_Music.Services;

/// <summary>
/// 网易云音乐专辑爬虫服务
/// 提供专辑搜索、简介、发行时间、发行公司等信息获取功能
/// 使用 AngleSharp 提供更优雅的 HTML 解析体验
/// </summary>
public class NetEaseAlbumCrawler : IDisposable
{
    private readonly IBrowsingContext _browsingContext;
    private readonly NetEaseAlbumCrawlerOptions _options;
    private bool _disposed;

    // 常量
    private const string SEARCH_URL_TEMPLATE =
        "https://music.163.com/api/search/get/web?csrf_token=&s={0}&type=10&limit=5&offset=0";
    private const string ALBUM_URL_TEMPLATE = "https://music.163.com/album?id={0}";

    // 使用 CSS 选择器替代 XPath，更简洁高效
    private static readonly string[] _descSelectors =
    [
        "#album-desc-dot p",
        ".album-desc p",
        ".desc p",
        "div[class*='album-desc'] p",
        "div[class*='desc'] p",
    ];

    private const string PUBLISH_TIME_LABEL = "发行时间";
    private const string COMPANY_LABEL = "发行公司";
    private const string INTR_SELECTOR = "p.intr";

    // 缓存JsonSerializerOptions，避免每次new
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// 构造函数
    /// </summary>
    public NetEaseAlbumCrawler(NetEaseAlbumCrawlerOptions? options = null)
    {
        _options = options ?? new NetEaseAlbumCrawlerOptions();

        // 配置 AngleSharp 浏览器上下文，利用其强大的配置能力
        var config = Configuration
            .Default.WithDefaultLoader(
                new LoaderOptions
                {
                    IsResourceLoadingEnabled = false, // 禁用资源加载以提高性能
                }
            )
            .WithDefaultCookies();

        _browsingContext = BrowsingContext.New(config);
    }

    /// <summary>
    /// 通过专辑名称获取专辑ID（可选艺人名）
    /// </summary>
    public async Task<string?> GetAlbumIdByNameAsync(string albumName, string? artistName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(albumName);
        try
        {
            string url = BuildSearchUrl(albumName);
            string json = await SendRequestAsync(url);
            return ParseSearchResults(json, albumName, artistName);
        }
        catch (Exception ex)
        {
            throw new NetEaseAlbumCrawlerException($"获取专辑ID失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 通过专辑ID获取专辑详情（简介、发行时间、发行公司）
    /// </summary>
    public async Task<AlbumDetail> GetAlbumDetailAsync(string albumId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(albumId);
        try
        {
            string url = BuildAlbumUrl(albumId);

            // 使用 AngleSharp 的内置 HTTP 客户端，自动处理编码、重定向等
            var document = await _browsingContext.OpenAsync(url);

            // 验证文档是否成功加载
            if (document == null)
            {
                throw new NetEaseAlbumCrawlerException("无法加载专辑页面");
            }

            return ParseAlbumDetail(document);
        }
        catch (Exception ex)
        {
            throw new NetEaseAlbumCrawlerException($"获取专辑详情失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 通过专辑名称直接获取专辑详情（简介、发行时间、发行公司）
    /// </summary>
    public async Task<AlbumDetail> GetAlbumDetailByNameAsync(string albumName, string? artistName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(albumName);
        string? albumId = await GetAlbumIdByNameAsync(albumName, artistName);
        if (albumId == null)
        {
            return new AlbumDetail { Description = _options.NotFoundMessage };
        }
        return await GetAlbumDetailAsync(albumId);
    }

    /// <summary>
    /// 构建搜索URL
    /// </summary>
    private static string BuildSearchUrl(string searchKeyword) =>
        string.Format(SEARCH_URL_TEMPLATE, System.Web.HttpUtility.UrlEncode(searchKeyword));

    /// <summary>
    /// 构建专辑页面URL
    /// </summary>
    private static string BuildAlbumUrl(string albumId) => string.Format(ALBUM_URL_TEMPLATE, albumId);

    /// <summary>
    /// 发送HTTP请求并返回响应内容
    /// </summary>
    private async Task<string> SendRequestAsync(string url)
    {
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(_options.RequestTimeoutSeconds);
        httpClient.DefaultRequestHeaders.Add("User-Agent", _options.UserAgent);
        httpClient.DefaultRequestHeaders.Add(
            "Accept",
            "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8"
        );
        httpClient.DefaultRequestHeaders.Add("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8");

        var response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// 解析搜索结果，返回专辑idStr
    /// </summary>
    private static string? ParseSearchResults(string json, string albumName, string? artistName)
    {
        var searchResult = JsonSerializer.Deserialize<AlbumSearchResult>(json, _jsonOptions);
        var albums = searchResult?.Result?.Albums;
        if (albums == null || albums.Count == 0)
            return null;

        if (artistName == null)
            return albums[0].IdStr;

        // 使用 LINQ 简化查找逻辑，利用 AngleSharp 的强类型特性
        var matchedAlbum = albums.FirstOrDefault(album =>
            !string.IsNullOrEmpty(album.Name)
            && album.Artist != null
            && !string.IsNullOrEmpty(album.Artist.Name)
            && IsAlbumMatch(album.Name, albumName)
            && IsArtistMatch(album.Artist.Name, artistName)
        );

        return matchedAlbum?.IdStr ?? albums[0].IdStr;
    }

    /// <summary>
    /// 解析专辑详情（简介、发行时间、发行公司）
    /// 利用 AngleSharp 的强大选择器功能
    /// </summary>
    private static AlbumDetail ParseAlbumDetail(IDocument document)
    {
        var detail = new AlbumDetail
        {
            Description = FindDescription(document) ?? "未找到专辑简介", // 使用 AngleSharp 的 CSS 选择器查找简介
        };

        // 解析发行时间和公司信息
        ParsePublishInfo(document, detail);

        return detail;
    }

    /// <summary>
    /// 查找专辑简介
    /// 利用 AngleSharp 的 CSS 选择器优势
    /// </summary>
    private static string? FindDescription(IDocument document)
    {
        // 使用多个选择器尝试查找简介，AngleSharp 的 CSS 选择器更简洁高效
        return _descSelectors
            .Select(document.QuerySelector)
            .OfType<IElement>()
            .Select(element => element.TextContent.Trim())
            .FirstOrDefault();
    }

    /// <summary>
    /// 解析发行信息（时间和公司）
    /// 利用 AngleSharp 的 DOM 操作优势
    /// </summary>
    private static void ParsePublishInfo(IDocument document, AlbumDetail detail)
    {
        // 使用 AngleSharp 的 QuerySelectorAll 获取所有匹配元素
        var intrElements = document.QuerySelectorAll(INTR_SELECTOR);

        foreach (var element in intrElements)
        {
            // 使用 AngleSharp 的 QuerySelector 查找子元素
            var boldElement = element.QuerySelector("b");
            if (boldElement == null)
                continue;

            string label = boldElement.TextContent.Trim();
            if (string.IsNullOrWhiteSpace(label))
                continue;

            // 获取完整文本并移除标签部分
            string fullText = element.TextContent.Trim();
            string value = fullText.Replace(label, "").Trim();

            // 使用 switch 表达式简化逻辑，AngleSharp 的强类型 API 使代码更安全
            _ = label switch
            {
                _ when label.Contains(PUBLISH_TIME_LABEL) => detail.PublishTime = value,
                _ when label.Contains(COMPANY_LABEL) => detail.Company = value,
                _ => string.Empty,
            };
        }
    }

    /// <summary>
    /// 专辑名模糊匹配
    /// </summary>
    private static bool IsAlbumMatch(string foundAlbum, string searchAlbum)
    {
        if (string.IsNullOrWhiteSpace(foundAlbum) || string.IsNullOrWhiteSpace(searchAlbum))
            return false;

        string normalizedFound = NormalizeString(foundAlbum);
        string normalizedSearch = NormalizeString(searchAlbum);

        return normalizedFound.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)
            || normalizedSearch.Contains(normalizedFound, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 艺术家名模糊匹配
    /// </summary>
    private static bool IsArtistMatch(string foundArtist, string searchArtist)
    {
        if (string.IsNullOrWhiteSpace(foundArtist) || string.IsNullOrWhiteSpace(searchArtist))
            return false;

        string normalizedFound = NormalizeString(foundArtist);
        string normalizedSearch = NormalizeString(searchArtist);

        return normalizedFound.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)
            || normalizedSearch.Contains(normalizedFound, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 字符串归一化
    /// </summary>
    private static string NormalizeString(string input) =>
        input.Replace(" ", "").Replace("-", "").Replace("_", "").Replace(".", "");

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

        _browsingContext.Dispose();
        _disposed = true;
    }
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

public class AlbumSearchResult
{
    public ResultData? Result { get; set; }
}

public class ResultData
{
    public List<AlbumItem>? Albums { get; set; }
}

public class AlbumItem
{
    public string? IdStr { get; set; }
    public string? Name { get; set; }
    public ArtistItem? Artist { get; set; }
}

public class ArtistItem
{
    public string? Name { get; set; }
}

public class AlbumDetail
{
    public string Description { get; set; } = string.Empty;
    public string PublishTime { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
}
