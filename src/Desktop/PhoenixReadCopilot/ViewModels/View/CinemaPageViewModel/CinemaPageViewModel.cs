using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Pages;
using PhoenixReadCopilot.Pages.Overlay;
using PhoenixReadCopilot.Services;
using PhoenixReadCopilot.ViewModels.Components;
using PhoenixReadCopilot.ViewModels.Core;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 找剧 / 多维筛选页面视图模型 (原影院页面).
/// </summary>
public sealed partial class CinemaPageViewModel : LayoutPageViewModelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CinemaPageViewModel"/> class.
    /// </summary>
    public CinemaPageViewModel(
        PhoenixReadApiClient apiClient,
        ILogger<CinemaPageViewModel> logger,
        NavigationViewModel navigationViewModel,
        DispatcherQueue dispatcherQueue,
        ISettingsService settingsService,
        ILoggerFactory loggerFactory
    )
        : base(settingsService)
    {
        _apiClient = apiClient;
        _logger = logger;
        _navigationViewModel = navigationViewModel;
        _dispatcherQueue = dispatcherQueue;
        _loggerFactory = loggerFactory;
    }

    /// <inheritdoc/>
    protected override string GetPageKey() => nameof(CinemaPage);

    [RelayCommand]
    private void Initialize()
    {
        if (Sections?.Count > 0)
        {
            return;
        }

        Sections =
        [
            new(
                PgcSectionType.TV,
                _apiClient,
                _dispatcherQueue,
                _loggerFactory.CreateLogger<EntertainmentIndexViewModel>()
            ),
        ];

        EntertainmentIndexViewModel section = Sections.First();
        _dispatcherQueue.TryEnqueue(
            DispatcherQueuePriority.Low,
            () =>
            {
                SelectSection(section);
                SectionInitialized?.Invoke(this, EventArgs.Empty);
            }
        );
    }

    [RelayCommand]
    private void SelectSection(EntertainmentIndexViewModel section)
    {
        if (SelectedSection?.Equals(section) == true)
        {
            return;
        }

        SelectedSection = section;
        _settings.CinemaPageLastSelectedSectionType = section.SectionType;
        section.InitializeCommand.Execute(null);
    }

    [RelayCommand]
    private void ShowMyFavoriteCinema()
    {
        _settings.LastSelectedFavoriteSection = "drama";
        _navigationViewModel.NavigateToOver(typeof(FavoritesPage));
    }
}
