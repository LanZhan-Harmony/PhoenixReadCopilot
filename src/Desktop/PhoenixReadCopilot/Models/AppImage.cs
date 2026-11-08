using PhoenixReadApi;

namespace PhoenixReadCopilot.Models;

/// <summary>
/// 应用内图片模型.
/// </summary>
public sealed class AppImage
{
    /// <summary>
    /// 图片链接.
    /// </summary>
    public Uri Uri { get; set; }

    /// <summary>
    /// 图片宽度.
    /// </summary>
    public double Width { get; set; }

    /// <summary>
    /// 图片高度.
    /// </summary>
    public double Height { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AppImage"/> class.
    /// </summary>
    public AppImage(Uri uri, double width = 0, double height = 0)
    {
        Uri = uri;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AppImage"/> class.
    /// </summary>
    public AppImage(string url, double width = 0, double height = 0)
        : this(new Uri(url), width, height) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AppImage"/> class from <see cref="CommentImage"/>.
    /// </summary>
    public AppImage(CommentImage commentImage)
        : this(new Uri(commentImage.Url), commentImage.Width, commentImage.Height) { }
}
