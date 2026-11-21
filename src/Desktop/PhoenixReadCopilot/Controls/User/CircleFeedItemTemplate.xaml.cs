using Microsoft.UI.Xaml;
using PhoenixReadCopilot.Controls.Components;

namespace PhoenixReadCopilot.Controls.User;

/// <summary>
/// 红果圈卡片数据模板字典.
/// </summary>
public sealed partial class CircleFeedItemTemplate : ResourceDictionary
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CircleFeedItemTemplate"/> class.
    /// </summary>
    public CircleFeedItemTemplate()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 当卡片容器被 ItemsRepeater 回收移出视口时释放图片资源，防止离屏容器常驻显存.
    /// </summary>
    public static void ClearImage(FrameworkElement container)
    {
        CoverImage.Clear(container);
    }
}
