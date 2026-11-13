using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.ViewModels.Core;
using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 收藏页面视图模型.
/// </summary>
public sealed partial class FavoritesPageViewModel
{
    private readonly PhoenixReadApiClient _apiClient;
    private readonly ILogger<FavoritesPageViewModel> _logger;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly ISettingsService _settingsService;
    private readonly NavigationViewModel _navigationViewModel;
    private readonly ILoggerFactory _loggerFactory;

    [ObservableProperty]
    public partial IFavoriteSectionDetailViewModel? CurrentSection { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    /// <summary>
    /// 分区初始化完成.
    /// </summary>
    public event EventHandler? SectionInitialized;

    /// <summary>
    /// 分区列表.
    /// </summary>
    public ObservableCollection<IFavoriteSectionDetailViewModel> Sections { get; } = [];
}
