using CommunityToolkit.Mvvm.ComponentModel;
using Richasy.BiliKernel.Models.Appearance;

namespace PhoenixReadCopilot.UI.ViewModels.View;

/// <summary>
/// 图片库页面视图模型.
/// </summary>
public sealed partial class GalleryPageViewModel
{
    [ObservableProperty]
    public partial List<BiliImage>? Images { get; set; }

    [ObservableProperty]
    public partial bool IsGroup { get; set; }

    [ObservableProperty]
    public partial bool IsMenuHide { get; set; }

    [ObservableProperty]
    public partial BiliImage SelectedImage { get; set; }
}
