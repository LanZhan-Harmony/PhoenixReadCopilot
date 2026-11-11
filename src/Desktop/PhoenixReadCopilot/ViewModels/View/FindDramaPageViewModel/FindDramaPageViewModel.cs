using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.Pages;
using PhoenixReadCopilot.Services;
using PhoenixReadCopilot.ViewModels.Core;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 找剧 / 多维筛选页面视图模型.
/// </summary>
public sealed partial class FindDramaPageViewModel : LayoutPageViewModelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FindDramaPageViewModel"/> class.
    /// </summary>
    public FindDramaPageViewModel(
        PhoenixReadApiClient apiClient,
        ILogger<FindDramaPageViewModel> logger,
        NavigationViewModel navigationViewModel,
        DispatcherQueue dispatcherQueue,
        ISettingsService settingsService
    )
        : base(settingsService)
    {
        _apiClient = apiClient;
        _logger = logger;
        _navigationViewModel = navigationViewModel;
        _dispatcherQueue = dispatcherQueue;
    }

    /// <inheritdoc/>
    protected override string GetPageKey() => nameof(FindDramaPage);
}
