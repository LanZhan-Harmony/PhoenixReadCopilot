using Microsoft.UI.Xaml;
using PhoenixReadCopilot.Controls.Components;

namespace PhoenixReadCopilot.Controls.Search;

/// <summary>
/// 搜索卡片数据模板字典.
/// </summary>
public sealed partial class SearchVideoItemTemplate : ResourceDictionary
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SearchVideoItemTemplate"/> class.
    /// </summary>
    public SearchVideoItemTemplate()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 当卡片容器被 ItemsRepeater 回收移出视口时释放图片资源，防止离屏容器常驻显存.
    /// </summary>
    public static void ClearImage(FrameworkElement container)
    {
        VideoCoverImage.Clear(container);
    }
}
