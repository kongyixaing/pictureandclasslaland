# PictureDisplayPlugin - 图片展示组件

ClassIsland 插件，在主界面上展示图片并自动轮播，支持本地文件夹和远程 API 两种图片源。

## 功能特性

- **本地文件夹模式**：扫描指定文件夹中的图片文件并轮播展示
- **远程 API 模式**：从远程 API 获取图片 URL 列表并轮播展示
  - 支持 **Picture & Videos API**（kongyixaing/picture classlaland-plugin-version 分支）
  - 支持 GitHub Contents API 格式
  - 支持 JSON URL 数组格式
  - 支持直接图片 URL
- **图片轮播**：可配置切换间隔（1-300 秒），支持随机播放
- **显示选项**：拉伸模式、背景颜色、圆角、显示图片名称
- **自动刷新**：设置变更后自动重新加载图片列表

## 安装

1. 将编译生成的 `PictureDisplayPlugin` 文件夹放入 ClassIsland 的插件目录
2. 重启 ClassIsland
3. 在【应用设置】→【组件】中找到"图片展示"组件，将其拖动到主界面

## 配置说明

### 图片来源

- **本地文件夹**：选择一个包含图片的文件夹路径，支持 `.png .jpg .jpeg .gif .bmp .webp .ico .tiff .tga` 格式
- **远程 API**：输入 API 地址，选择返回格式

### Picture & Videos API（推荐）

本插件专为 [Picture & Videos](https://github.com/kongyixaing/picture/tree/classlaland-plugin-version) 应用提供支持。

#### 配置步骤

1. 部署 Picture & Videos 服务（本地或服务器）
2. 在组件设置中选择"远程 API"
3. API 类型选择"Picture & Videos API"
4. 填写 API 服务器地址，如 `http://localhost:5001`
5. 填写用户名和密码（需要登录才能获取图片）

#### API 接口

| 接口 | 说明 |
|------|------|
| `/pic/api/picture/random` | 随机获取一张已审核图片 |
| `/pic/api/picture/list` | 分页获取图片列表 |
| `/pic/api/picture/<id>` | 按 ID 获取指定图片 |

> 默认获取前 100 张已审核的图片，自动过滤视频文件。

### 其他 API 格式

#### GitHub Contents API

使用 GitHub 仓库 Contents API 获取文件列表。URL 格式：
```
https://api.github.com/repos/{owner}/{repo}/contents?ref={branch}
```

#### JSON URL 数组

API 返回一个 JSON 字符串数组，包含图片 URL：
```json
["https://example.com/img1.png", "https://example.com/img2.jpg"]
```

#### 直接图片

API URL 本身就是一张图片，直接展示该图片。

### 显示选项

| 选项 | 说明 |
|------|------|
| 图片切换间隔 | 1-300 秒，默认 30 秒 |
| 图片拉伸模式 | 填充 / 等比缩放 / 等比填充裁切 / 不缩放 |
| 背景颜色 | ARGB 十六进制，如 `#FF000000`（不透明黑色） |
| 圆角半径 | 0-50 像素 |
| 随机播放 | 启用后随机切换图片顺序 |
| 显示图片名称 | 在图片底部显示当前图片的标题或文件名 |

## 开发环境

### 前置要求

- .NET 8.0 SDK
- JetBrains Rider 或 Visual Studio 2022
- Git
- PowerShell Core

### 搭建步骤

1. 克隆 ClassIsland 源码并构建：
   ```bash
   git clone https://github.com/ClassIsland/ClassIsland.git
   cd ClassIsland
   git submodule update --init --recursive
   pwsh ./tools/plugin/build.ps1
   ```

2. 编辑 `Properties/launchSettings.json`，将路径替换为你的 ClassIsland 构建输出目录

3. 在 IDE 中选择对应的启动配置文件，启动调试

## 项目结构

```
PictureDisplayPlugin/
├── PictureDisplayPlugin.csproj    # 项目文件
├── manifest.yml                   # 插件清单
├── Plugin.cs                      # 插件入口点
├── Enums/                         # 枚举定义
│   ├── PictureSourceType.cs       # 图片来源类型
│   ├── ImageStretchMode.cs        # 图片拉伸模式
│   └── RemoteApiFormat.cs         # 远程 API 格式
├── Models/                        # 数据模型
│   └── PictureDisplaySettings.cs  # 组件设置模型
├── Services/                      # 服务
│   └── PictureService.cs          # 图片加载服务
├── Components/                    # 组件
│   ├── PictureDisplayComponent.axaml(.cs)       # 图片展示组件
│   └── PictureDisplaySettingsControl.axaml(.cs) # 设置控件
├── Properties/                    # 项目属性
│   └── launchSettings.json        # 调试启动配置
└── README.md                      # 自述文件
```

## 技术栈

- .NET 8.0
- Avalonia UI 11.3
- ClassIsland Plugin SDK 2.0.0.1

## 相关仓库

- [Picture & Videos](https://github.com/kongyixaing/picture/tree/classlaland-plugin-version) - 图片视频分享社区，本插件的远程图片源
- [ClassIsland](https://github.com/ClassIsland/ClassIsland) - 班级信息展示工具

## 许可证

GPL-3.0
