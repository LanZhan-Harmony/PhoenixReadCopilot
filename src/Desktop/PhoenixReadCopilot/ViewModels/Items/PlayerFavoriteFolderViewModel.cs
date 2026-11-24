using CommunityToolkit.Mvvm.ComponentModel;
using PhoenixReadCopilot.Core.Models.Media;

namespace PhoenixReadCopilot.ViewModels.Items;

/// <summary>
/// 用户收藏夹视图模型.
/// </summary>
internal sealed partial class PlayerFavoriteFolderViewModel : ViewModelBase
{
    /// <summary>
    /// 收藏夹数据实体.
    /// </summary>
    public VideoFavoriteFolder Data { get; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public PlayerFavoriteFolderViewModel(VideoFavoriteFolder data, bool isSelected)
    {
        Data = data;
        IsSelected = isSelected;
    }
}
