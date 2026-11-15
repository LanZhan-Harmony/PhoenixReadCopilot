using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Models.Media;
using PhoenixReadCopilot.Pages.Overlay;
using PhoenixReadCopilot.ViewModels.Components;
using PhoenixReadCopilot.ViewModels.Core;
using PhoenixReadCopilot.ViewModels.Items;
using ZLogger;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 收藏页面视图模型.
/// </summary>
public sealed partial class FavoritesPageViewModel : LayoutPageViewModelBase
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

    public async Task InitializeAsync()
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
            _logger.ZLogError(ex, $"初始化追剧收藏夹失败");
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
        _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low,
            async () =>
            {
                Sections.Clear();
                await InitializeAsync();
            });
    }

    public void SelectSection(IFavoriteSectionDetailViewModel? vm)
    {
        if (vm is null || vm == CurrentSection)
        {
            return;
        }

        CurrentSection = vm;
        _ = CurrentSection.TryFirstLoadAsync();
    }

    private void RestoreSelection()
    {
        if (CurrentSection is null && Sections.Count > 0)
        {
            SelectSection(Sections.First());
        }
    }
}
