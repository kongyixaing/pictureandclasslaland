using System.ComponentModel;
using System.Runtime.CompilerServices;
using PictureDisplayPlugin.Enums;

namespace PictureDisplayPlugin.Models;

/// <summary>
/// 图片展示组件的设置模型
/// </summary>
public class PictureDisplaySettings : INotifyPropertyChanged
{
    private PictureSourceType _sourceType = PictureSourceType.LocalFolder;
    private string _localFolderPath = "";
    private string _apiBaseUrl = "";
    private RemoteApiFormat _apiFormat = RemoteApiFormat.PictureVideosApi;
    private string _apiUsername = "";
    private string _apiPassword = "";
    private int _refreshIntervalSeconds = 30;
    private ImageStretchMode _stretchMode = ImageStretchMode.UniformToFill;
    private string _backgroundColor = "#00000000";
    private bool _shuffle = false;
    private bool _showFileName = false;
    private int _cornerRadius = 0;

    /// <summary>
    /// 图片来源类型
    /// </summary>
    public PictureSourceType SourceType
    {
        get => _sourceType;
        set => SetField(ref _sourceType, value);
    }

    /// <summary>
    /// 本地图片文件夹路径
    /// </summary>
    public string LocalFolderPath
    {
        get => _localFolderPath;
        set => SetField(ref _localFolderPath, value);
    }

    /// <summary>
    /// 远程 API 基础地址（如 http://localhost:5001）
    /// </summary>
    public string ApiBaseUrl
    {
        get => _apiBaseUrl;
        set => SetField(ref _apiBaseUrl, value);
    }

    /// <summary>
    /// 远程 API 返回格式
    /// </summary>
    public RemoteApiFormat ApiFormat
    {
        get => _apiFormat;
        set => SetField(ref _apiFormat, value);
    }

    /// <summary>
    /// API 用户名（Picture &amp; Videos API 需要）
    /// </summary>
    public string ApiUsername
    {
        get => _apiUsername;
        set => SetField(ref _apiUsername, value);
    }

    /// <summary>
    /// API 密码（Picture &amp; Videos API 需要）
    /// </summary>
    public string ApiPassword
    {
        get => _apiPassword;
        set => SetField(ref _apiPassword, value);
    }

    /// <summary>
    /// 图片切换间隔（秒）
    /// </summary>
    public int RefreshIntervalSeconds
    {
        get => _refreshIntervalSeconds;
        set => SetField(ref _refreshIntervalSeconds, value < 1 ? 1 : value);
    }

    /// <summary>
    /// 图片拉伸模式
    /// </summary>
    public ImageStretchMode StretchMode
    {
        get => _stretchMode;
        set => SetField(ref _stretchMode, value);
    }

    /// <summary>
    /// 背景颜色（ARGB 十六进制）
    /// </summary>
    public string BackgroundColor
    {
        get => _backgroundColor;
        set => SetField(ref _backgroundColor, value);
    }

    /// <summary>
    /// 是否随机播放
    /// </summary>
    public bool Shuffle
    {
        get => _shuffle;
        set => SetField(ref _shuffle, value);
    }

    /// <summary>
    /// 是否显示文件名
    /// </summary>
    public bool ShowFileName
    {
        get => _showFileName;
        set => SetField(ref _showFileName, value);
    }

    /// <summary>
    /// 圆角半径
    /// </summary>
    public int CornerRadius
    {
        get => _cornerRadius;
        set => SetField(ref _cornerRadius, value < 0 ? 0 : value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
