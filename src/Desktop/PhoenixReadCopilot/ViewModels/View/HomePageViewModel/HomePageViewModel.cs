using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Pages;
using PhoenixReadCopilot.Pages.Overlay;
using PhoenixReadCopilot.ViewModels.Core;
using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 首页视图模型.
/// </summary>
public sealed partial class HomePageViewModel : LayoutPageViewModelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HomePageViewModel"/> class.
    /// </summary>
    public HomePageViewModel(
        PhoenixReadApiClient apiClient,
        ILogger<HomePageViewModel> logger,
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
    protected override string GetPageKey() => nameof(HomePage);

    [RelayCommand]
    private async Task InitializeAsync()
    {
        if (Videos.Count > 0)
        {
            return;
        }

        await LoadVideosAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        IsVideoLoading = false;

        Videos.Clear();
        _recommendOffset = 0;
        _recommendSessionId = null;
        _seenDramaIds.Clear();
        _hasMoreRecommendations = true;

        await LoadVideosAsync();
    }

    [RelayCommand]
    private void PlayCuratedList()
    {
        VideoItemViewModel? first = Videos.FirstOrDefault();
        if (first?.Drama is not null)
        {
            _navigationViewModel.NavigateToOver(typeof(VideoPlayerPage), first.Drama);
        }
    }
}
