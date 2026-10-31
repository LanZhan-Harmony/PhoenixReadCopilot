using CommunityToolkit.Mvvm.ComponentModel;
using Richasy.BiliKernel.Models.Media;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.UI.ViewModels.Items;

/// <summary>
/// 用户收藏夹视图模型.
/// </summary>
public sealed partial class PlayerFavoriteFolderViewModel : ViewModelBase<VideoFavoriteFolder>
{
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerFavoriteFolderViewModel"/> class.
    /// </summary>
    public PlayerFavoriteFolderViewModel(VideoFavoriteFolder data, bool isSelected)
        : base(data)
    {
        IsSelected = isSelected;
    }
}
