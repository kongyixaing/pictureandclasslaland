using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Media.Imaging;
using PictureDisplayPlugin.Enums;
using PictureDisplayPlugin.Models;

namespace PictureDisplayPlugin.Services;

/// <summary>
/// 图片加载服务，负责从本地文件夹或远程 API 获取并管理图片
/// </summary>
public class PictureService : IDisposable
{
    private static readonly HttpClientHandler HttpClientHandler = new()
    {
        UseCookies = true,
        CookieContainer = new CookieContainer(),
        AllowAutoRedirect = true
    };

    private static readonly HttpClient HttpClient = new(HttpClientHandler)
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    private static readonly string[] ImageExtensions =
    {
        ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp", ".ico", ".tiff", ".tga"
    };

    private List<PictureItem> _imageList = new();
    private int _currentIndex = 0;
    private bool _disposed = false;
    private string? _lastApiBaseUrl;
    private string? _lastUsername;
    private string? _lastPassword;
    private bool _isLoggedIn;

    /// <summary>
    /// 当前图片列表
    /// </summary>
    public IReadOnlyList<PictureItem> ImageList => _imageList;

    /// <summary>
    /// 当前图片索引
    /// </summary>
    public int CurrentIndex => _currentIndex;

    /// <summary>
    /// 当前图片信息
    /// </summary>
    public PictureItem? CurrentImage => _imageList.Count > 0 ? _imageList[_currentIndex] : null;

    /// <summary>
    /// 图片列表是否为空
    /// </summary>
    public bool IsEmpty => _imageList.Count == 0;

    /// <summary>
    /// 是否已登录（仅 Picture &amp; Videos API）
    /// </summary>
    public bool IsLoggedIn => _isLoggedIn;

    /// <summary>
    /// 从本地文件夹加载图片列表
    /// </summary>
    /// <param name="folderPath">文件夹路径</param>
    /// <returns>找到的图片数量</returns>
    public async Task<int> LoadFromLocalFolderAsync(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
        {
            _imageList.Clear();
            _currentIndex = 0;
            return 0;
        }

        await Task.Run(() =>
        {
            var files = Directory.GetFiles(folderPath)
                .Where(f => ImageExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .OrderBy(f => f)
                .Select(f => new PictureItem
                {
                    Url = f,
                    FileName = Path.GetFileName(f),
                    Title = Path.GetFileNameWithoutExtension(f)
                })
                .ToList();
            _imageList = files;
            _currentIndex = 0;
        });

        return _imageList.Count;
    }

    /// <summary>
    /// 从远程 API 加载图片列表
    /// </summary>
    /// <param name="apiBaseUrl">API 基础地址</param>
    /// <param name="format">API 返回格式</param>
    /// <param name="username">用户名（PictureVideosApi 需要）</param>
    /// <param name="password">密码（PictureVideosApi 需要）</param>
    /// <returns>找到的图片数量</returns>
    public async Task<int> LoadFromRemoteApiAsync(string apiBaseUrl, RemoteApiFormat format,
        string? username = null, string? password = null)
    {
        if (string.IsNullOrWhiteSpace(apiBaseUrl))
        {
            _imageList.Clear();
            _currentIndex = 0;
            return 0;
        }

        try
        {
            switch (format)
            {
                case RemoteApiFormat.PictureVideosApi:
                    return await LoadFromPictureVideosApiAsync(apiBaseUrl, username, password);

                case RemoteApiFormat.DirectImage:
                    _imageList = new List<PictureItem>
                    {
                        new() { Url = apiBaseUrl, FileName = GetFileNameFromUrl(apiBaseUrl) }
                    };
                    break;

                case RemoteApiFormat.JsonUrlArray:
                {
                    var response = await HttpClient.GetStringAsync(apiBaseUrl);
                    var urls = JsonSerializer.Deserialize<List<string>>(response);
                    _imageList = urls?
                        .Where(IsImageUrl)
                        .Select(u => new PictureItem
                        {
                            Url = u,
                            FileName = GetFileNameFromUrl(u),
                            Title = GetFileNameFromUrl(u)
                        })
                        .ToList() ?? new List<PictureItem>();
                    break;
                }

                case RemoteApiFormat.GitHubContentsApi:
                {
                    var response = await HttpClient.GetStringAsync(apiBaseUrl);
                    var items = JsonSerializer.Deserialize<List<GitHubContentItem>>(response);
                    _imageList = items?
                        .Where(i => i.Type == "file" && IsImageUrl(i.DownloadUrl ?? i.Name ?? ""))
                        .Select(i => new PictureItem
                        {
                            Url = i.DownloadUrl ?? "",
                            FileName = i.Name ?? "",
                            Title = i.Name ?? ""
                        })
                        .Where(p => !string.IsNullOrEmpty(p.Url))
                        .ToList() ?? new List<PictureItem>();
                    break;
                }
            }

            _currentIndex = 0;
        }
        catch (Exception)
        {
            _imageList.Clear();
            _currentIndex = 0;
        }

        return _imageList.Count;
    }

    /// <summary>
    /// 从 Picture &amp; Videos API 加载图片列表
    /// </summary>
    private async Task<int> LoadFromPictureVideosApiAsync(string apiBaseUrl, string? username, string? password)
    {
        // 如果 URL 或凭证变更，重新登录
        if (_lastApiBaseUrl != apiBaseUrl || _lastUsername != username || _lastPassword != password)
        {
            _isLoggedIn = false;
            _lastApiBaseUrl = apiBaseUrl;
            _lastUsername = username;
            _lastPassword = password;
        }

        // 尝试登录（如果未登录且有凭证）
        if (!_isLoggedIn && !string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
        {
            _isLoggedIn = await LoginPictureVideosApiAsync(apiBaseUrl, username, password);
        }

        // 获取图片列表（前 100 张）
        var listUrl = NormalizeApiBaseUrl(apiBaseUrl) + "/api/picture/list?per_page=100";
        var response = await HttpClient.GetAsync(listUrl);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            // 未授权，尝试重新登录
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                _isLoggedIn = await LoginPictureVideosApiAsync(apiBaseUrl, username, password);
                if (_isLoggedIn)
                {
                    response = await HttpClient.GetAsync(listUrl);
                }
            }
        }

        if (!response.IsSuccessStatusCode)
        {
            _imageList.Clear();
            _currentIndex = 0;
            return 0;
        }

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PictureVideosListResponse>(json);

        if (result?.Pictures != null)
        {
            _imageList = result.Pictures
                .Where(p => !p.IsVideo && !string.IsNullOrEmpty(p.Url))
                .Select(p => new PictureItem
                {
                    Url = p.Url!,
                    FileName = p.Filename ?? GetFileNameFromUrl(p.Url ?? ""),
                    Title = p.Title ?? "",
                    Description = p.Description ?? "",
                    UploaderName = p.UploaderName ?? "",
                    Id = p.Id
                })
                .ToList();
        }
        else
        {
            _imageList.Clear();
        }

        _currentIndex = 0;
        return _imageList.Count;
    }

    /// <summary>
    /// 登录 Picture &amp; Videos API
    /// </summary>
    private async Task<bool> LoginPictureVideosApiAsync(string apiBaseUrl, string username, string password)
    {
        try
        {
            var loginUrl = NormalizeApiBaseUrl(apiBaseUrl) + "/login";
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("username", username),
                new KeyValuePair<string, string>("password", password)
            });

            var response = await HttpClient.PostAsync(loginUrl, content);

            // 登录成功通常会重定向或返回成功
            // Flask 登录后设置 session cookie，CookieContainer 会自动处理
            return response.IsSuccessStatusCode ||
                   response.StatusCode == System.Net.HttpStatusCode.Redirect ||
                   response.StatusCode == System.Net.HttpStatusCode.Found;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 从 Picture &amp; Videos API 获取随机图片
    /// </summary>
    public async Task<PictureItem?> GetRandomPictureFromApiAsync(string apiBaseUrl,
        string? username = null, string? password = null)
    {
        if (string.IsNullOrWhiteSpace(apiBaseUrl))
            return null;

        try
        {
            // 检查是否需要登录
            if (_lastApiBaseUrl != apiBaseUrl || _lastUsername != username || _lastPassword != password)
            {
                _isLoggedIn = false;
                _lastApiBaseUrl = apiBaseUrl;
                _lastUsername = username;
                _lastPassword = password;
            }

            if (!_isLoggedIn && !string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                _isLoggedIn = await LoginPictureVideosApiAsync(apiBaseUrl, username, password);
            }

            var randomUrl = NormalizeApiBaseUrl(apiBaseUrl) + "/api/picture/random";
            var response = await HttpClient.GetAsync(randomUrl);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                {
                    _isLoggedIn = await LoginPictureVideosApiAsync(apiBaseUrl, username, password);
                    if (_isLoggedIn)
                    {
                        response = await HttpClient.GetAsync(randomUrl);
                    }
                }
            }

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            var pic = JsonSerializer.Deserialize<PictureVideosPictureResponse>(json);

            if (pic == null || string.IsNullOrEmpty(pic.Url) || pic.IsVideo)
                return null;

            return new PictureItem
            {
                Url = pic.Url,
                FileName = pic.Filename ?? GetFileNameFromUrl(pic.Url),
                Title = pic.Title ?? "",
                Description = pic.Description ?? "",
                UploaderName = pic.UploaderName ?? "",
                Id = pic.Id
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 加载当前图片为 Bitmap
    /// </summary>
    /// <returns>图片 Bitmap，失败返回 null</returns>
    public async Task<Bitmap?> LoadCurrentImageAsync()
    {
        if (IsEmpty || CurrentImage?.Url == null)
            return null;

        return await LoadImageFromUrlAsync(CurrentImage.Url);
    }

    /// <summary>
    /// 从 URL 加载图片为 Bitmap
    /// </summary>
    public async Task<Bitmap?> LoadImageFromUrlAsync(string url)
    {
        if (string.IsNullOrEmpty(url))
            return null;

        try
        {
            if (File.Exists(url))
            {
                // 本地文件 - 在线程池上解码，避免阻塞 UI
                using var stream = File.OpenRead(url);
                return await Task.Run(() => new Bitmap(stream));
            }
            else
            {
                // 远程 URL - 下载到内存后在线程池上解码
                var bytes = await HttpClient.GetByteArrayAsync(url);
                using var ms = new MemoryStream(bytes);
                return await Task.Run(() => new Bitmap(ms));
            }
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 切换到下一张图片
    /// </summary>
    /// <returns>下一张图片信息</returns>
    public PictureItem? MoveNext(bool shuffle = false)
    {
        if (IsEmpty)
            return null;

        if (shuffle)
        {
            var rnd = new Random();
            var newIndex = rnd.Next(_imageList.Count);
            // 避免连续相同
            if (_imageList.Count > 1)
            {
                while (newIndex == _currentIndex)
                    newIndex = rnd.Next(_imageList.Count);
            }
            _currentIndex = newIndex;
        }
        else
        {
            _currentIndex = (_currentIndex + 1) % _imageList.Count;
        }

        return CurrentImage;
    }

    /// <summary>
    /// 切换到上一张图片
    /// </summary>
    public PictureItem? MovePrevious()
    {
        if (IsEmpty)
            return null;

        _currentIndex = _currentIndex == 0 ? _imageList.Count - 1 : _currentIndex - 1;
        return CurrentImage;
    }

    /// <summary>
    /// 获取当前图片的显示名称
    /// </summary>
    public string GetCurrentDisplayName()
    {
        if (CurrentImage == null)
            return "";

        if (!string.IsNullOrEmpty(CurrentImage.Title))
            return CurrentImage.Title;

        return CurrentImage.FileName ?? "";
    }

    /// <summary>
    /// 规范化 API 基础地址：去除尾部斜杠。
    /// 用户输入的地址应包含完整路径（如 http://example.com/pic），
    /// 代码在此基础上拼接 /login、/api/picture/list 等子路径。
    /// </summary>
    private static string NormalizeApiBaseUrl(string apiBaseUrl)
    {
        return apiBaseUrl.TrimEnd('/');
    }

    private static bool IsImageUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
            return false;

        var ext = Path.GetExtension(url).ToLowerInvariant();
        return ImageExtensions.Contains(ext);
    }

    private static string GetFileNameFromUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
            return "";

        try
        {
            var uri = new Uri(url);
            return Path.GetFileName(uri.LocalPath);
        }
        catch
        {
            return Path.GetFileName(url);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _imageList.Clear();
            _disposed = true;
        }
    }
}

/// <summary>
/// 图片项信息
/// </summary>
public class PictureItem
{
    public int Id { get; set; }
    public string Url { get; set; } = "";
    public string FileName { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string UploaderName { get; set; } = "";
    public bool IsVideo { get; set; }
}

#region API Response Models

/// <summary>
/// Picture &amp; Videos API 单张图片响应
/// </summary>
internal class PictureVideosPictureResponse
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("filename")]
    public string? Filename { get; set; }

    [JsonPropertyName("uploader_uid")]
    public string? UploaderUid { get; set; }

    [JsonPropertyName("uploader_name")]
    public string? UploaderName { get; set; }

    [JsonPropertyName("is_video")]
    public bool IsVideo { get; set; }

    [JsonPropertyName("upload_time")]
    public long UploadTime { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("code")]
    public int Code { get; set; }
}

/// <summary>
/// Picture &amp; Videos API 图片列表响应
/// </summary>
internal class PictureVideosListResponse
{
    [JsonPropertyName("pictures")]
    public List<PictureVideosPictureResponse>? Pictures { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("per_page")]
    public int PerPage { get; set; }

    [JsonPropertyName("total_pages")]
    public int TotalPages { get; set; }
}

/// <summary>
/// GitHub Contents API 文件项
/// </summary>
internal class GitHubContentItem
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("path")]
    public string? Path { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("download_url")]
    public string? DownloadUrl { get; set; }
}

#endregion
