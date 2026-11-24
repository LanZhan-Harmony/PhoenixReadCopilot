using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using PhoenixReadCopilot.Contracts.Services;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.ViewModels.Core;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 播放器页面视图模型基类.
/// </summary>
internal abstract partial class PlayerPageViewModelBase : LayoutPageViewModelBase
{
    protected readonly IWindowService _windowService;
    protected readonly ISettingsService _settingsService;

    [ObservableProperty]
    public partial PlayerViewModelBase Player { get; set; }

    [ObservableProperty]
    public partial bool IsSeparatorWindowPlayer { get; set; }

    protected PlayerPageViewModelBase(ISettingsService settingsService, IWindowService windowService)
        : base(settingsService)
    {
        _settingsService = settingsService;
        _windowService = windowService;
        PlayerType playerType = _settings.PlayerType;

        Player = playerType switch
        {
            PlayerType.FFmpeg => Ioc.Default.GetRequiredService<FFmpegPlayerViewModel>(),
            _ => Ioc.Default.GetRequiredService<FFmpegPlayerViewModel>(),
        };

        Player.AttachedWindow = windowService.ActiveWindow;
    }
}
