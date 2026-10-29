using CommunityToolkit.Mvvm.ComponentModel;
using PhoenixReadCopilot.UI.Models.Constants;
using PhoenixReadCopilot.UI.Toolkits;
using PhoenixReadCopilot.UI.ViewModels.Core;

namespace PhoenixReadCopilot.UI.ViewModels.View;

/// <summary>
/// 播放器页面视图模型基类.
/// </summary>
public abstract partial class PlayerPageViewModelBase : LayoutPageViewModelBase
{
    [ObservableProperty]
    private PlayerViewModelBase _player;

    [ObservableProperty]
    private bool _isSeparatorWindowPlayer;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerPageViewModelBase"/> class.
    /// </summary>
    protected PlayerPageViewModelBase()
    {
        var playerType = SettingsToolkit.ReadLocalSetting(
            SettingNames.PlayerType,
            PlayerType.Native
        );

        Player = playerType switch
        {
            PlayerType.Native => new NativePlayerViewModel(),
            PlayerType.FFmpeg => new FFmpegPlayerViewModel(),
            _ => new NativePlayerViewModel(),
        };

        Player.AttachedWindow = this.Get<AppViewModel>().ActivatedWindow;
    }
}
