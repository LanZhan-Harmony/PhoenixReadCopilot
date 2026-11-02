using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadCopilot.UI.Models.Constants;
using PhoenixReadCopilot.UI.Pages;
using PhoenixReadCopilot.UI.Pages.Overlay;
using PhoenixReadCopilot.UI.Services;
using PhoenixReadCopilot.UI.ViewModels.Components;
using PhoenixReadCopilot.UI.ViewModels.Core;
using PhoenixReadCopilot.UI.ViewModels.Items;
using Richasy.BiliKernel.Bili.Media;
using Richasy.BiliKernel.Models;

namespace PhoenixReadCopilot.UI.ViewModels.View;

/// <summary>
/// 动漫页面视图模型.
/// </summary>
public sealed partial class AnimePageViewModel : LayoutPageViewModelBase
{
    private readonly NavigationViewModel _navigationViewModel;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnimePageViewModel"/> class.
    /// </summary>
    public AnimePageViewModel(
        IEntertainmentDiscoveryService discoveryService,
        ILogger<AnimePageViewModel> logger,
        NavigationViewModel navigationViewModel,
        DispatcherQueue dispatcherQueue,
        ISettingsService settingsService,
        ILoggerFactory loggerFactory
    )
        : base(settingsService)
    {
        _service = discoveryService;
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

        Sections = new List<IPgcSectionDetailViewModel>
        {
            new AnimeTimelineViewModel(
                _service,
                _loggerFactory.CreateLogger<AnimeTimelineViewModel>()
            ),
            new EntertainmentIndexViewModel(
                PgcSectionType.Anime,
                _service,
                _dispatcherQueue,
                _loggerFactory.CreateLogger<EntertainmentIndexViewModel>()
            ),
        };

        var lastType = _settings.AnimePageLastSelectedSectionType;
        var section = Sections.FirstOrDefault(p => p.SectionType == lastType);
        _dispatcherQueue.TryEnqueue(
            DispatcherQueuePriority.Low,
            () =>
            {
                SelectSection(section ?? Sections.First());
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
        section.InitializeCommand.Execute(default);
    }

    [RelayCommand]
    private void ShowMyFavoriteAnime()
    {
        _settings.LastSelectedFavoriteSection = $"pgc_{PgcFavoriteType.Anime}";
        _navigationViewModel.NavigateToOver(typeof(FavoritesPage));
    }
}
