namespace PictureDisplayPlugin.Enums;

/// <summary>
/// 远程 API 返回格式
/// </summary>
public enum RemoteApiFormat
{
    /// <summary>
    /// Picture &amp; Videos API（kongyixaing/picture classlaland-plugin-version 分支）
    /// </summary>
    PictureVideosApi = 0,

    /// <summary>
    /// 直接图片 URL（API 返回的就是一张图片）
    /// </summary>
    DirectImage = 1,

    /// <summary>
    /// JSON 数组格式（返回 ["url1", "url2", ...]）
    /// </summary>
    JsonUrlArray = 2,

    /// <summary>
    /// GitHub Contents API 格式（返回文件列表 JSON）
    /// </summary>
    GitHubContentsApi = 3
}
