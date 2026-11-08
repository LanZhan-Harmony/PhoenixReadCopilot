using CommunityToolkit.Mvvm.ComponentModel;
using PhoenixReadCopilot.Models;
using PhoenixReadCopilot.ViewModels.Core;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 图片库页面视图模型.
/// </summary>
public sealed partial class GalleryPageViewModel
{
    private readonly AppViewModel _appViewModel;

    [ObservableProperty]
    public partial List<AppImage>? Images { get; set; }

    [ObservableProperty]
    public partial bool IsGroup { get; set; }

    [ObservableProperty]
    public partial bool IsMenuHide { get; set; }

    [ObservableProperty]
    public partial AppImage SelectedImage { get; set; }
}
