using Microsoft.UI.Xaml;
using PhoenixReadCopilot.Controls.Components;

namespace PhoenixReadCopilot.Controls.Player;

/// <summary>
/// 推荐短剧卡片数据模板字典.
/// </summary>
internal sealed partial class PlayerRecommendedDramaItemTemplate : ResourceDictionary
{
    public PlayerRecommendedDramaItemTemplate()
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
