using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadCopilot.UI.Pages.Overlay;
using PhoenixReadCopilot.UI.Services;
using PhoenixReadCopilot.UI.ViewModels.Components;
using PhoenixReadCopilot.UI.ViewModels.Core;
using PhoenixReadCopilot.UI.ViewModels.Items;
using Richasy.BiliKernel.Bili.User;
using Richasy.BiliKernel.Models;

namespace PhoenixReadCopilot.UI.ViewModels.View;

/// <summary>
/// 收藏页面视图模型.
/// </summary>
public sealed partial class FavoritesPageViewModel : LayoutPageViewModelBase
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly ISettingsService _settingsService;
    private readonly NavigationViewModel _navigationViewModel;
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="FavoritesPageViewModel"/> class.
    /// </summary>
    public FavoritesPageViewModel(
        IFavoriteService service,
        ILogger<FavoritesPageViewModel> logger,
        DispatcherQueue dispatcherQueue,
        ISettingsService settingsService,
        NavigationViewModel navigationViewModel,
        ILoggerFactory loggerFactory
    )
        : base(settingsService)
    {
        _service = service;
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
        Sections.Add(
            new PgcFavoriteSectionDetailViewModel(
                PgcFavoriteType.Anime,
                _service,
                _loggerFactory.CreateLogger<PgcFavoriteSectionDetailViewModel>()
            )
        );
        Sections.Add(
            new PgcFavoriteSectionDetailViewModel(
                PgcFavoriteType.Cinema,
                _service,
                _loggerFactory.CreateLogger<PgcFavoriteSectionDetailViewModel>()
            )
        );
        await InitializeVideoFoldersAsync();
        IsLoading = false;
        RestoreSelection();
        SectionInitialized?.Invoke(this, EventArgs.Empty);
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
    private void SelectSection(IFavoriteSectionDetailViewModel vm)
    {
        if (vm is null || vm == CurrentSection)
        {
            return;
        }

        var sectionSettingValue = string.Empty;
        if (vm is PgcFavoriteSectionDetailViewModel pgcVM)
        {
            sectionSettingValue = $"pgc_{pgcVM.Type}";
        }
        else if (vm is VideoFavoriteSectionDetailViewModel videoVM)
        {
            sectionSettingValue = $"video_{videoVM.Data.Id}";
        }
        else if (vm is UgcSeasonFavoriteSectionDetailViewModel ugcVM)
        {
            sectionSettingValue = $"ugc_{ugcVM.Data.Id}";
        }

        _settings.LastSelectedFavoriteSection = sectionSettingValue;
        CurrentSection = vm;
        CurrentSection.TryFirstLoadCommand.Execute(default);
    }

    private void RestoreSelection()
    {
        var lastSelectedSection = _settings.LastSelectedFavoriteSection;
        if (string.IsNullOrEmpty(lastSelectedSection))
        {
            SelectSection(Sections.First());
            return;
        }

        var isSelected = false;
        if (lastSelectedSection.StartsWith("pgc"))
        {
            var isSuccess = Enum.TryParse<PgcFavoriteType>(
                lastSelectedSection.Replace("pgc_", string.Empty),
                out var type
            );
            if (isSuccess)
            {
                var section = Sections
                    .OfType<PgcFavoriteSectionDetailViewModel>()
                    .FirstOrDefault(p => p.Type == type);
                if (section is not null)
                {
                    isSelected = true;
                    SelectSection(section);
                }
            }
        }
        else if (lastSelectedSection.StartsWith("video"))
        {
            var id = lastSelectedSection.Replace("video_", string.Empty);
            var section = Sections
                .OfType<VideoFavoriteSectionDetailViewModel>()
                .FirstOrDefault(p => p.Data.Id == id);
            if (section is not null)
            {
                isSelected = true;
                SelectSection(section);
            }
        }
        else if (lastSelectedSection.StartsWith("ugc"))
        {
            var id = lastSelectedSection.Replace("ugc_", string.Empty);
            var section = Sections
                .OfType<UgcSeasonFavoriteSectionDetailViewModel>()
                .FirstOrDefault(p => p.Data.Id == id);
            if (section is not null)
            {
                isSelected = true;
                SelectSection(section);
            }
        }

        if (!isSelected && CurrentSection is null)
        {
            SelectSection(Sections.First());
        }
    }

    private async Task InitializeVideoFoldersAsync()
    {
        try
        {
            var (groups, defaultGroup) = await _service.GetVideoFavoriteGroupsAsync();
            var defaultGroupVM = new VideoFavoriteSectionDetailViewModel(
                defaultGroup.Folder,
                _service,
                _navigationViewModel,
                _settingsService,
                _loggerFactory.CreateLogger<VideoFavoriteSectionDetailViewModel>()
            );
            defaultGroupVM.InjectFirstPageData(defaultGroup);
            Sections.Add(defaultGroupVM);

            foreach (var group in groups)
            {
                foreach (var folder in group.Folders)
                {
                    var folderVM =
                        folder.IsUgcSeason == true
                            ? (IFavoriteSectionDetailViewModel)
                            new UgcSeasonFavoriteSectionDetailViewModel(
                                folder,
                                _service,
                                _loggerFactory.CreateLogger<UgcSeasonFavoriteSectionDetailViewModel>()
                            )
                            : new VideoFavoriteSectionDetailViewModel(
                                folder,
                                _service,
                                _navigationViewModel,
                                _settingsService,
                                _loggerFactory.CreateLogger<VideoFavoriteSectionDetailViewModel>()
                            );
                    Sections.Add(folderVM);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化视频收藏夹失败");
        }
    }
}
