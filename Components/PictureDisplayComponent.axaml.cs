using System.ComponentModel;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using PictureDisplayPlugin.Enums;
using PictureDisplayPlugin.Models;
using PictureDisplayPlugin.Services;

namespace PictureDisplayPlugin.Components;

/// <summary>
/// 图片展示组件，在主界面上展示图片并自动轮播
/// </summary>
[ComponentInfo(
    "E7A2F3B1-4C5D-4A6B-9D8E-1F2A3B4C5D6E",
    "图片展示",
    "\uEB9F",
    "在主界面上展示图片，支持本地文件夹和远程 API 两种图片源。"
)]
public partial class PictureDisplayComponent : ComponentBase<PictureDisplaySettings>
{
    private readonly PictureService _pictureService = new();
    private readonly DispatcherTimer _timer = new();
    private bool _isLoaded = false;
    private bool _isLoading = false;

    public PictureDisplayComponent()
    {
        InitializeComponent();

        _timer.Tick += OnTimerTick;
        Loaded += OnComponentLoaded;
        Unloaded += OnComponentUnloaded;
    }

    /// <summary>
    /// 组件加载时（添加到视觉树后）调用，此时 Settings 已可用
    /// </summary>
    private void OnComponentLoaded(object? sender, RoutedEventArgs e)
    {
        _isLoaded = true;

        if (Settings == null)
            return;

        // 订阅设置变更
        Settings.PropertyChanged += OnSettingsPropertyChanged;

        // 应用初始设置
        ApplyAllSettings();

        // 加载图片并开始轮播
        _ = RefreshImageListAsync();
    }

    private void OnComponentUnloaded(object? sender, RoutedEventArgs e)
    {
        _isLoaded = false;
        _timer.Stop();

        // 释放当前图片
        (ImageDisplay.Source as IDisposable)?.Dispose();
        ImageDisplay.Source = null;

        if (Settings != null)
        {
            Settings.PropertyChanged -= OnSettingsPropertyChanged;
        }

        _pictureService.Dispose();
    }

    /// <summary>
    /// 设置变更回调
    /// </summary>
    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!_isLoaded || Settings == null)
            return;

        var propName = e.PropertyName;

        // 需要重新加载图片列表的变更
        if (propName == nameof(PictureDisplaySettings.SourceType) ||
            propName == nameof(PictureDisplaySettings.LocalFolderPath) ||
            propName == nameof(PictureDisplaySettings.ApiBaseUrl) ||
            propName == nameof(PictureDisplaySettings.ApiFormat) ||
            propName == nameof(PictureDisplaySettings.ApiUsername) ||
            propName == nameof(PictureDisplaySettings.ApiPassword))
        {
            _ = RefreshImageListAsync();
            return;
        }

        // 刷新间隔变更
        if (propName == nameof(PictureDisplaySettings.RefreshIntervalSeconds))
        {
            UpdateTimer();
            return;
        }

        // 拉伸模式变更
        if (propName == nameof(PictureDisplaySettings.StretchMode))
        {
            UpdateStretchMode();
            return;
        }

        // 背景色变更
        if (propName == nameof(PictureDisplaySettings.BackgroundColor))
        {
            UpdateBackground();
            return;
        }

        // 随机播放变更
        if (propName == nameof(PictureDisplaySettings.Shuffle))
            return; // 下次切换时自动生效

        // 显示文件名变更
        if (propName == nameof(PictureDisplaySettings.ShowFileName))
        {
            UpdateFileNameDisplay();
            return;
        }

        // 圆角变更
        if (propName == nameof(PictureDisplaySettings.CornerRadius))
        {
            UpdateCornerRadius();
            return;
        }
    }

    /// <summary>
    /// 应用所有初始设置
    /// </summary>
    private void ApplyAllSettings()
    {
        if (Settings == null)
            return;

        UpdateStretchMode();
        UpdateBackground();
        UpdateFileNameDisplay();
        UpdateCornerRadius();
        UpdateTimer();
    }

    /// <summary>
    /// 刷新图片列表
    /// </summary>
    private async Task RefreshImageListAsync()
    {
        if (!_isLoaded || Settings == null)
            return;

        _timer.Stop();

        try
        {
            int count;
            if (Settings.SourceType == PictureSourceType.LocalFolder)
            {
                count = await _pictureService.LoadFromLocalFolderAsync(Settings.LocalFolderPath);
            }
            else
            {
                count = await _pictureService.LoadFromRemoteApiAsync(
                    Settings.ApiBaseUrl, Settings.ApiFormat,
                    Settings.ApiUsername, Settings.ApiPassword);
            }

            if (count > 0)
            {
                await LoadAndDisplayCurrentImageAsync();
                UpdateTimer();
                _timer.Start();
            }
            else
            {
                ShowPlaceholder("暂无图片");
            }
        }
        catch
        {
            ShowPlaceholder("加载失败");
        }
    }

    /// <summary>
    /// 加载并显示当前图片
    /// </summary>
    private async Task LoadAndDisplayCurrentImageAsync()
    {
        if (_isLoading)
            return;

        _isLoading = true;

        try
        {
            var bitmap = await _pictureService.LoadCurrentImageAsync();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (bitmap != null)
                {
                    // 释放旧图片，避免内存泄漏
                    (ImageDisplay.Source as IDisposable)?.Dispose();
                    ImageDisplay.Source = bitmap;
                    ImageDisplay.IsVisible = true;
                    PlaceholderText.IsVisible = false;

                    if (Settings != null && Settings.ShowFileName)
                    {
                        FileNameText.Text = _pictureService.GetCurrentDisplayName();
                        FileNameBorder.IsVisible = true;
                    }
                }
                else
                {
                    ShowPlaceholder("图片加载失败");
                }
            });
        }
        catch
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ShowPlaceholder("图片加载失败");
            });
        }
        finally
        {
            _isLoading = false;
        }
    }

    /// <summary>
    /// 显示占位文本
    /// </summary>
    private void ShowPlaceholder(string text)
    {
        ImageDisplay.IsVisible = false;
        FileNameBorder.IsVisible = false;
        PlaceholderText.Text = text;
        PlaceholderText.IsVisible = true;
    }

    /// <summary>
    /// 定时器回调：切换到下一张图片
    /// </summary>
    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (!_isLoaded || Settings == null || _pictureService.IsEmpty)
            return;

        var shuffle = Settings.Shuffle;
        _pictureService.MoveNext(shuffle);
        _ = LoadAndDisplayCurrentImageAsync();
    }

    /// <summary>
    /// 更新定时器间隔
    /// </summary>
    private void UpdateTimer()
    {
        if (Settings == null)
            return;

        _timer.Interval = TimeSpan.FromSeconds(Settings.RefreshIntervalSeconds);
    }

    /// <summary>
    /// 更新图片拉伸模式
    /// </summary>
    private void UpdateStretchMode()
    {
        if (Settings == null)
            return;

        ImageDisplay.Stretch = Settings.StretchMode switch
        {
            ImageStretchMode.Fill => Avalonia.Media.Stretch.Fill,
            ImageStretchMode.Uniform => Avalonia.Media.Stretch.Uniform,
            ImageStretchMode.UniformToFill => Avalonia.Media.Stretch.UniformToFill,
            _ => Avalonia.Media.Stretch.None
        };
    }

    /// <summary>
    /// 更新背景颜色
    /// </summary>
    private void UpdateBackground()
    {
        if (Settings == null)
            return;

        try
        {
            var color = Color.Parse(Settings.BackgroundColor);
            BackgroundBorder.Background = new SolidColorBrush(color);
        }
        catch
        {
            BackgroundBorder.Background = Brushes.Transparent;
        }
    }

    /// <summary>
    /// 更新文件名显示
    /// </summary>
    private void UpdateFileNameDisplay()
    {
        if (Settings == null)
            return;

        if (!Settings.ShowFileName)
        {
            FileNameBorder.IsVisible = false;
        }
        else if (!_pictureService.IsEmpty)
        {
            FileNameText.Text = _pictureService.GetCurrentDisplayName();
            FileNameBorder.IsVisible = true;
        }
    }

    /// <summary>
    /// 更新圆角
    /// </summary>
    private void UpdateCornerRadius()
    {
        if (Settings == null)
            return;

        var radius = Settings.CornerRadius;
        BackgroundBorder.CornerRadius = new CornerRadius(radius);
    }
}
