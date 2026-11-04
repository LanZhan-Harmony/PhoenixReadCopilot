using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadCopilot.UI.Models.Constants;
using PhoenixReadCopilot.UI.Services;
using PhoenixReadCopilot.UI.ViewModels.Core;

namespace PhoenixReadCopilot.UI.ViewModels.View;

/// <summary>
/// 播放器页面视图模型基类.
/// </summary>
public abstract partial class PlayerPageViewModelBase : LayoutPageViewModelBase
{
    protected readonly AppViewModel _appViewModel;
    protected readonly ISettingsService _settingsService;

    [ObservableProperty]
    public partial PlayerViewModelBase Player { get; set; }

    [ObservableProperty]
    public partial bool IsSeparatorWindowPlayer { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerPageViewModelBase"/> class.
    /// </summary>
    protected PlayerPageViewModelBase(ISettingsService settingsService, AppViewModel appViewModel)
        : base(settingsService)
    {
        _settingsService = settingsService;
        _appViewModel = appViewModel;
        var playerType = _settings.PlayerType;

        Player = playerType switch
        {
            PlayerType.FFmpeg => Ioc.Default.GetService<FFmpegPlayerViewModel>(),
            _ => Ioc.Default.GetService<FFmpegPlayerViewModel>(),
        };

        Player.AttachedWindow = appViewModel.ActivatedWindow;
    }
}
