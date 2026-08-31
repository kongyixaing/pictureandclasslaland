namespace PictureDisplayPlugin.Enums;

/// <summary>
/// 图片拉伸模式
/// </summary>
public enum ImageStretchMode
{
    /// <summary>
    /// 填充（可能变形）
    /// </summary>
    Fill = 0,

    /// <summary>
    /// 等比缩放（完整显示）
    /// </summary>
    Uniform = 1,

    /// <summary>
    /// 等比填充裁切
    /// </summary>
    UniformToFill = 2,

    /// <summary>
    /// 不缩放
    /// </summary>
    None = 3
}
