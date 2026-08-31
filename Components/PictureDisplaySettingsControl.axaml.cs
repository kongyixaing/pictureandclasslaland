using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ClassIsland.Core.Abstractions.Controls;
using PictureDisplayPlugin.Enums;
using PictureDisplayPlugin.Models;

namespace PictureDisplayPlugin.Components;

/// <summary>
/// 图片展示组件的设置控件
/// </summary>
public partial class PictureDisplaySettingsControl : ComponentBase<PictureDisplaySettings>
{
    public PictureDisplaySettingsControl()
    {
        InitializeComponent();
        Loaded += OnControlLoaded;
        Unloaded += OnControlUnloaded;
    }

    private void OnControlLoaded(object? sender, RoutedEventArgs e)
    {
        if (Settings == null)
            return;

        // 加载当前设置到控件
        LoadSettingsToControls();

        // 订阅控件事件
        SetupControlEvents();

        // 订阅设置变更（用于外部更新）
        Settings.PropertyChanged += OnSettingsPropertyChanged;
    }

    private void OnControlUnloaded(object? sender, RoutedEventArgs e)
    {
        if (Settings != null)
        {
            Settings.PropertyChanged -= OnSettingsPropertyChanged;
        }
    }

    /// <summary>
    /// 将当前 Settings 值加载到控件
    /// </summary>
    private void LoadSettingsToControls()
    {
        if (Settings == null)
            return;

        SourceTypeComboBox.SelectedIndex = (int)Settings.SourceType;
        LocalFolderPathTextBox.Text = Settings.LocalFolderPath;
        ApiBaseUrlTextBox.Text = Settings.ApiBaseUrl;
        ApiFormatComboBox.SelectedIndex = (int)Settings.ApiFormat;
        ApiUsernameTextBox.Text = Settings.ApiUsername;
        ApiPasswordTextBox.Text = Settings.ApiPassword;
        RefreshIntervalSlider.Value = Settings.RefreshIntervalSeconds;
        RefreshIntervalText.Text = $"{Settings.RefreshIntervalSeconds}s";
        StretchModeComboBox.SelectedIndex = (int)Settings.StretchMode;
        BackgroundColorTextBox.Text = Settings.BackgroundColor;
        CornerRadiusSlider.Value = Settings.CornerRadius;
        CornerRadiusText.Text = $"{Settings.CornerRadius}px";
        ShuffleCheckBox.IsChecked = Settings.Shuffle;
        ShowFileNameCheckBox.IsChecked = Settings.ShowFileName;

        UpdatePanelVisibility();
    }

    /// <summary>
    /// 设置控件事件
    /// </summary>
    private void SetupControlEvents()
    {
        SourceTypeComboBox.SelectionChanged += (_, _) =>
        {
            if (Settings == null) return;
            Settings.SourceType = (PictureSourceType)SourceTypeComboBox.SelectedIndex;
            UpdatePanelVisibility();
        };

        LocalFolderPathTextBox.LostFocus += (_, _) =>
        {
            if (Settings != null)
                Settings.LocalFolderPath = LocalFolderPathTextBox.Text ?? "";
        };

        ApiBaseUrlTextBox.LostFocus += (_, _) =>
        {
            if (Settings != null)
                Settings.ApiBaseUrl = ApiBaseUrlTextBox.Text ?? "";
        };

        ApiFormatComboBox.SelectionChanged += (_, _) =>
        {
            if (Settings != null)
            {
                Settings.ApiFormat = (RemoteApiFormat)ApiFormatComboBox.SelectedIndex;
                UpdateAuthPanelVisibility();
            }
        };

        ApiUsernameTextBox.LostFocus += (_, _) =>
        {
            if (Settings != null)
                Settings.ApiUsername = ApiUsernameTextBox.Text ?? "";
        };

        ApiPasswordTextBox.LostFocus += (_, _) =>
        {
            if (Settings != null)
                Settings.ApiPassword = ApiPasswordTextBox.Text ?? "";
        };

        RefreshIntervalSlider.PropertyChanged += (_, e) =>
        {
            if (e.Property == Slider.ValueProperty && Settings != null)
            {
                var val = (int)RefreshIntervalSlider.Value;
                Settings.RefreshIntervalSeconds = val;
                RefreshIntervalText.Text = $"{val}s";
            }
        };

        StretchModeComboBox.SelectionChanged += (_, _) =>
        {
            if (Settings != null)
                Settings.StretchMode = (ImageStretchMode)StretchModeComboBox.SelectedIndex;
        };

        BackgroundColorTextBox.LostFocus += (_, _) =>
        {
            if (Settings != null)
                Settings.BackgroundColor = BackgroundColorTextBox.Text ?? "";
        };

        CornerRadiusSlider.PropertyChanged += (_, e) =>
        {
            if (e.Property == Slider.ValueProperty && Settings != null)
            {
                var val = (int)CornerRadiusSlider.Value;
                Settings.CornerRadius = val;
                CornerRadiusText.Text = $"{val}px";
            }
        };

        ShuffleCheckBox.IsCheckedChanged += (_, _) =>
        {
            if (Settings != null)
                Settings.Shuffle = ShuffleCheckBox.IsChecked ?? false;
        };

        ShowFileNameCheckBox.IsCheckedChanged += (_, _) =>
        {
            if (Settings != null)
                Settings.ShowFileName = ShowFileNameCheckBox.IsChecked ?? false;
        };
    }

    /// <summary>
    /// 外部设置变更时同步控件
    /// </summary>
    private void OnSettingsPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (Settings == null)
            return;

        switch (e.PropertyName)
        {
            case nameof(PictureDisplaySettings.SourceType):
                SourceTypeComboBox.SelectedIndex = (int)Settings.SourceType;
                UpdatePanelVisibility();
                break;

            case nameof(PictureDisplaySettings.LocalFolderPath):
                if (LocalFolderPathTextBox.Text != Settings.LocalFolderPath)
                    LocalFolderPathTextBox.Text = Settings.LocalFolderPath;
                break;

            case nameof(PictureDisplaySettings.ApiBaseUrl):
                if (ApiBaseUrlTextBox.Text != Settings.ApiBaseUrl)
                    ApiBaseUrlTextBox.Text = Settings.ApiBaseUrl;
                break;

            case nameof(PictureDisplaySettings.ApiFormat):
                ApiFormatComboBox.SelectedIndex = (int)Settings.ApiFormat;
                UpdateAuthPanelVisibility();
                break;

            case nameof(PictureDisplaySettings.ApiUsername):
                if (ApiUsernameTextBox.Text != Settings.ApiUsername)
                    ApiUsernameTextBox.Text = Settings.ApiUsername;
                break;

            case nameof(PictureDisplaySettings.ApiPassword):
                if (ApiPasswordTextBox.Text != Settings.ApiPassword)
                    ApiPasswordTextBox.Text = Settings.ApiPassword;
                break;

            case nameof(PictureDisplaySettings.RefreshIntervalSeconds):
                RefreshIntervalSlider.Value = Settings.RefreshIntervalSeconds;
                RefreshIntervalText.Text = $"{Settings.RefreshIntervalSeconds}s";
                break;

            case nameof(PictureDisplaySettings.StretchMode):
                StretchModeComboBox.SelectedIndex = (int)Settings.StretchMode;
                break;

            case nameof(PictureDisplaySettings.BackgroundColor):
                BackgroundColorTextBox.Text = Settings.BackgroundColor;
                break;

            case nameof(PictureDisplaySettings.CornerRadius):
                CornerRadiusSlider.Value = Settings.CornerRadius;
                CornerRadiusText.Text = $"{Settings.CornerRadius}px";
                break;

            case nameof(PictureDisplaySettings.Shuffle):
                ShuffleCheckBox.IsChecked = Settings.Shuffle;
                break;

            case nameof(PictureDisplaySettings.ShowFileName):
                ShowFileNameCheckBox.IsChecked = Settings.ShowFileName;
                break;
        }
    }

    /// <summary>
    /// 根据来源类型更新面板可见性
    /// </summary>
    private void UpdatePanelVisibility()
    {
        var isLocal = SourceTypeComboBox.SelectedIndex == 0;
        LocalFolderPanel.IsVisible = isLocal;
        RemoteApiPanel.IsVisible = !isLocal;

        if (!isLocal)
        {
            UpdateAuthPanelVisibility();
        }
    }

    /// <summary>
    /// 根据 API 格式更新认证面板可见性
    /// </summary>
    private void UpdateAuthPanelVisibility()
    {
        var format = (RemoteApiFormat)ApiFormatComboBox.SelectedIndex;
        var needsAuth = format == RemoteApiFormat.PictureVideosApi;
        ApiAuthPanel.IsVisible = needsAuth;
        ApiPasswordPanel.IsVisible = needsAuth;
    }

    /// <summary>
    /// 浏览文件夹按钮点击
    /// </summary>
    private async void BrowseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null)
            return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "选择图片文件夹",
            AllowMultiple = false
        });

        if (folders.Count > 0)
        {
            var path = folders[0].Path.LocalPath;
            LocalFolderPathTextBox.Text = path;

            if (Settings != null)
                Settings.LocalFolderPath = path;
        }
    }
}
