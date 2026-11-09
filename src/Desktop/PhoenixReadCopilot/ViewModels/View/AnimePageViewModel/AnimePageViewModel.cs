using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.Pages;
using PhoenixReadCopilot.Pages.Overlay;
using PhoenixReadCopilot.Services;
using PhoenixReadCopilot.ViewModels.Components;
using PhoenixReadCopilot.ViewModels.Core;
using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 上新日历页面视图模型 (原动漫页面).
/// </summary>
public sealed partial class AnimePageViewModel : LayoutPageViewModelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AnimePageViewModel"/> class.
    /// </summary>
    public AnimePageViewModel(
        PhoenixReadApiClient apiClient,
        ILogger<AnimePageViewModel> logger,
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
    protected override string GetPageKey() => nameof(AnimePage);

    [RelayCommand]
    private void Initialize()
    {
        if (Sections?.Count > 0)
        {
            return;
        }

        Sections =
        [
            new AnimeTimelineViewModel(
                _apiClient,
                _dispatcherQueue,
                _loggerFactory.CreateLogger<AnimeTimelineViewModel>()
            ),
        ];

        IPgcSectionDetailViewModel section = Sections.First();
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
    private void SelectSection(IPgcSectionDetailViewModel section)
    {
        if (SelectedSection?.Equals(section) == true)
        {
            return;
        }

        SelectedSection = section;
        _settings.AnimePageLastSelectedSectionType = section.SectionType;
        section.InitializeCommand.Execute(null);
    }

    [RelayCommand]
    private void ShowMyFavoriteAnime()
    {
        _settings.LastSelectedFavoriteSection = "drama";
        _navigationViewModel.NavigateToOver(typeof(FavoritesPage));
    }
}
