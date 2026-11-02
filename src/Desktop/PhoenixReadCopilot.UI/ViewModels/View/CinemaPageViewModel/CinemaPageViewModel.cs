using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadCopilot.UI.Models.Constants;
using PhoenixReadCopilot.UI.Pages;
using PhoenixReadCopilot.UI.Pages.Overlay;
using PhoenixReadCopilot.UI.Services;
using PhoenixReadCopilot.UI.ViewModels.Components;
using PhoenixReadCopilot.UI.ViewModels.Core;
using Richasy.BiliKernel.Bili.Media;
using Richasy.BiliKernel.Models;

namespace PhoenixReadCopilot.UI.ViewModels.View;

/// <summary>
/// 影院页面视图模型.
/// </summary>
public sealed partial class CinemaPageViewModel : LayoutPageViewModelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CinemaPageViewModel"/> class.
    /// </summary>
    public CinemaPageViewModel(
        IEntertainmentDiscoveryService discoveryService,
        ILogger<CinemaPageViewModel> logger,
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
    protected override string GetPageKey() => nameof(CinemaPage);

    [RelayCommand]
    private void Initialize()
    {
        if (Sections?.Count > 0)
        {
            return;
        }

        Sections = new List<EntertainmentIndexViewModel>
        {
            new EntertainmentIndexViewModel(
                PgcSectionType.Movie,
                _service,
                _dispatcherQueue,
                _loggerFactory.CreateLogger<EntertainmentIndexViewModel>()
            ),
            new EntertainmentIndexViewModel(
                PgcSectionType.TV,
                _service,
                _dispatcherQueue,
                _loggerFactory.CreateLogger<EntertainmentIndexViewModel>()
            ),
            new EntertainmentIndexViewModel(
                PgcSectionType.Documentary,
                _service,
                _dispatcherQueue,
                _loggerFactory.CreateLogger<EntertainmentIndexViewModel>()
            ),
        };
        var lastType = _settings.CinemaPageLastSelectedSectionType;
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
    private void SelectSection(EntertainmentIndexViewModel section)
    {
        if (SelectedSection?.Equals(section) == true)
        {
            return;
        }

        SelectedSection = section;
        _settings.CinemaPageLastSelectedSectionType = section.SectionType;
        section.InitializeCommand.Execute(default);
    }

    [RelayCommand]
    private void ShowMyFavoriteCinema()
    {
        _settings.LastSelectedFavoriteSection = $"pgc_{PgcFavoriteType.Cinema}";
        _navigationViewModel.NavigateToOver(typeof(FavoritesPage));
    }
}
