using PhoenixReadApi;

namespace PhoenixReadCopilot.Core.Models.Appearance;

/// <summary>
/// 应用内图片模型.
/// </summary>
public sealed class AppImage(Uri uri, double width = 0, double height = 0)
{
    /// <summary>
    /// 图片链接.
    /// </summary>
    public Uri Uri { get; set; } = uri;

    /// <summary>
    /// 图片宽度.
    /// </summary>
    public double Width { get; set; } = width;

    /// <summary>
    /// 图片高度.
    /// </summary>
    public double Height { get; set; } = height;

    public AppImage(string url, double width = 0, double height = 0)
        : this(new Uri(url), width, height) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AppImage"/> class from <see cref="CommentImage"/>.
    /// </summary>
    public AppImage(CommentImage commentImage)
        : this(new Uri(commentImage.Url), commentImage.Width, commentImage.Height) { }
}
