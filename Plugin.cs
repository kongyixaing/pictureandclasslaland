using ClassIsland.Core.Abstractions;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Extensions.Registry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PictureDisplayPlugin.Components;

namespace PictureDisplayPlugin;

/// <summary>
/// 图片展示插件入口
/// </summary>
[PluginEntrance]
public class Plugin : PluginBase
{
    public override void Initialize(HostBuilderContext context, IServiceCollection services)
    {
        // 注册图片展示组件及其设置控件
        services.AddComponent<PictureDisplayComponent, PictureDisplaySettingsControl>();
    }
}
