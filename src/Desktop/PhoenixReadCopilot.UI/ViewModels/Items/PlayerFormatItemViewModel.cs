using CommunityToolkit.Mvvm.ComponentModel;
using PhoenixReadCopilot.UI.ViewModels.Components;
using Richasy.BiliKernel.Models.Media;
using Richasy.WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.UI.ViewModels.Items;

/// <summary>
/// 播放器画质项视图模型.
/// </summary>
public sealed partial class PlayerFormatItemViewModel : ViewModelBase<PlayerFormatInformation>
{
    [ObservableProperty]
    private bool _isEnabled;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerFormatItemViewModel"/> class.
    /// </summary>
    public PlayerFormatItemViewModel(PlayerFormatInformation data)
        : base(data)
    {
        var userIsVip = this.Get<AccountViewModel>().MyProfile.IsVip ?? false;
        IsEnabled = !data.NeedVip || (data.NeedVip && userIsVip);
    }
}
