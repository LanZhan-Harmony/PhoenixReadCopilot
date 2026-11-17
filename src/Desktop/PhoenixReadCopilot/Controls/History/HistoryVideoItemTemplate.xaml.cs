using Microsoft.UI.Xaml;
using PhoenixReadCopilot.Controls.Components;

namespace PhoenixReadCopilot.Controls.History;

/// <summary>
/// 搜索卡片数据模板字典.
/// </summary>
public sealed partial class HistoryVideoItemTemplate : ResourceDictionary
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HistoryVideoItemTemplate"/> class.
    /// </summary>
    public HistoryVideoItemTemplate()
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
