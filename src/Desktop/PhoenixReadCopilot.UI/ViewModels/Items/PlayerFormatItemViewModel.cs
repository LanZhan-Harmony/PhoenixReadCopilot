using CommunityToolkit.Mvvm.ComponentModel;
using Richasy.BiliKernel.Models.Media;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.UI.ViewModels.Items;

/// <summary>
/// 播放器画质项视图模型.
/// </summary>
public sealed partial class PlayerFormatItemViewModel : ViewModelBase<PlayerFormatInformation>
{
    [ObservableProperty]
    public partial bool IsEnabled { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerFormatItemViewModel"/> class.
    /// </summary>
    public PlayerFormatItemViewModel(PlayerFormatInformation data)
        : base(data)
    {
        var userIsVip = ItemServices.Account.MyProfile.IsVip ?? false;
        IsEnabled = !data.NeedVip || (data.NeedVip && userIsVip);
    }
}
