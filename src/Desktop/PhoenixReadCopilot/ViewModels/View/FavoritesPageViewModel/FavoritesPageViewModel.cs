using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.Pages.Overlay;
using PhoenixReadCopilot.Services;
using PhoenixReadCopilot.ViewModels.Components;
using PhoenixReadCopilot.ViewModels.Core;
using PhoenixReadCopilot.ViewModels.Items;
using PhoenixReadCopilot.Core.Models.Media;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 收藏页面视图模型.
/// </summary>
public sealed partial class FavoritesPageViewModel : LayoutPageViewModelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FavoritesPageViewModel"/> class.
    /// </summary>
    public FavoritesPageViewModel(
        PhoenixReadApiClient apiClient,
        ILogger<FavoritesPageViewModel> logger,
        DispatcherQueue dispatcherQueue,
        ISettingsService settingsService,
        NavigationViewModel navigationViewModel,
        ILoggerFactory loggerFactory
    )
        : base(settingsService)
    {
        _apiClient = apiClient;
        _logger = logger;
        _dispatcherQueue = dispatcherQueue;
        _settingsService = settingsService;
        _navigationViewModel = navigationViewModel;
        _loggerFactory = loggerFactory;
    }

    /// <inheritdoc/>
    protected override string GetPageKey() => nameof(FavoritesPage);

    [RelayCommand]
    private async Task InitializeAsync()
    {
        if (Sections.Count > 0)
        {
            RestoreSelection();
            SectionInitialized?.Invoke(this, EventArgs.Empty);
            return;
        }

        IsLoading = true;
        try
        {
            VideoFavoriteFolder defaultFolder = new("favorites", "追剧收藏", null, null, null, 0);
            VideoFavoriteSectionDetailViewModel defaultSection = new(
                defaultFolder,
                _apiClient,
                _navigationViewModel,
                _settingsService,
                _loggerFactory.CreateLogger<VideoFavoriteSectionDetailViewModel>()
            );
            Sections.Add(defaultSection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化追剧收藏夹失败");
        }
        finally
        {
            IsLoading = false;
        }

        RestoreSelection();
        SectionInitialized?.Invoke(this, EventArgs.Empty);
        await Task.CompletedTask;
    }

    [RelayCommand]
    private void Refresh()
    {
        _dispatcherQueue.TryEnqueue(
            DispatcherQueuePriority.Low,
            async () =>
            {
                Sections.Clear();
                await InitializeAsync();
            }
        );
    }

    [RelayCommand]
    private void SelectSection(IFavoriteSectionDetailViewModel? vm)
    {
        if (vm is null || vm == CurrentSection)
        {
            return;
        }

        CurrentSection = vm;
        CurrentSection.TryFirstLoadCommand.Execute(null);
    }

    private void RestoreSelection()
    {
        if (CurrentSection is null && Sections.Count > 0)
        {
            SelectSection(Sections.First());
        }
    }
}
