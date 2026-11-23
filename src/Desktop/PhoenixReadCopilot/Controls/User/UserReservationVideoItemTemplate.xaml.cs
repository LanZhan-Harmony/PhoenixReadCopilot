using Microsoft.UI.Xaml;
using PhoenixReadCopilot.Controls.Components;

namespace PhoenixReadCopilot.Controls.User;

/// <summary>
/// 他人空间作品预约卡片数据模板字典（模仿上新日历待播卡片风格，保留预约状态交互）.
/// </summary>
internal sealed partial class UserReservationVideoItemTemplate : ResourceDictionary
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserReservationVideoItemTemplate"/> class.
    /// </summary>
    public UserReservationVideoItemTemplate()
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
