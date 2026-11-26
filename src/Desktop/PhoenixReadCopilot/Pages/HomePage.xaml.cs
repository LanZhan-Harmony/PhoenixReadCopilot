using System.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.Controls.Home;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Helpers.Animations;
using PhoenixReadCopilot.ViewModels.View;
using WinUI3Package;

namespace PhoenixReadCopilot.Pages;

/// <summary>
/// 首页（推荐短剧与新剧推荐）页面.
/// </summary>
internal sealed partial class HomePage : Page
{
    private const double CarouselPauseScrollOffset = 460;

    private readonly INavigationService _navigationService =
        Ioc.Default.GetRequiredService<INavigationService>();
    private readonly List<FrameworkElement> _pendingRepositionElements = [];

    private bool _isUpdatingSelector;
    private bool _isRepositionFlushScheduled;

    /// <summary>
    /// 视图模型.
    /// </summary>
    public HomePageViewModel ViewModel { get; } =
        Ioc.Default.GetRequiredService<HomePageViewModel>();

    public HomePage()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        SyncSelectorBar();
        CategorySelector.SelectionChanged += OnCategorySelectionChanged;
        ViewModel.VideoListUpdated += OnVideoListUpdatedAsync;
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        _navigationService.OverlayStateChanged += OnOverlayStateChanged;

        UpdateCarouselPlaybackState();
        _ = ViewModel.InitializeAsync();
        CheckVideoCount();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        CategorySelector.SelectionChanged -= OnCategorySelectionChanged;
        ViewModel.VideoListUpdated -= OnVideoListUpdatedAsync;
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _navigationService.OverlayStateChanged -= OnOverlayStateChanged;
        Carousel.Pause();
        _pendingRepositionElements.Clear();
        _isRepositionFlushScheduled = false;
    }

    private void OnOverlayStateChanged(object? sender, bool isOverlayOpen) =>
        UpdateCarouselPlaybackState();

    private void UpdateCarouselPlaybackState()
    {
        if (
            _navigationService.IsOverlayOpen
            || VideoScrollView.VerticalOffset > CarouselPauseScrollOffset
        )
        {
            Carousel.Pause();
        }
        else
        {
            Carousel.Resume();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(HomePageViewModel.SelectedSection))
        {
            SyncSelectorBar();
        }
    }

    private void SyncSelectorBar()
    {
        HomeSectionType activeType = ViewModel.SelectedSection?.SectionType
            ?? HomeSectionType.Recommend;
        SelectorBarItem target =
            activeType == HomeSectionType.NewArrival
                ? NewArrivalSelectorItem
                : RecommendSelectorItem;

        if (CategorySelector.SelectedItem == target)
        {
            return;
        }

        try
        {
            _isUpdatingSelector = true;
            CategorySelector.SelectedItem = target;
        }
        finally
        {
            _isUpdatingSelector = false;
        }
    }

    private void OnCategorySelectionChanged(
        SelectorBar sender,
        SelectorBarSelectionChangedEventArgs args
    )
    {
        if (_isUpdatingSelector)
        {
            return;
        }

        if (
            sender.SelectedItem?.Tag is string tag
            && Enum.TryParse<HomeSectionType>(tag, out HomeSectionType sectionType)
        )
        {
            _ = ViewModel.SelectSectionAsync(sectionType);
        }
    }

    private async void OnVideoListUpdatedAsync(object? sender, EventArgs e)
    {
        try
        {
            await Task.Delay(500);
            CheckVideoCount();
        }
        catch
        {
            // 页面卸载后忽略延迟调度异常
        }
    }

    private void OnViewChanged(object? sender, ScrollViewerViewChangedEventArgs args)
    {
        UpdateCarouselPlaybackState();

        if (
            ViewModel is { IsVideoLoading: false, HasMoreCurrentVideos: true }
            && VideoScrollView.ExtentHeight
                - VideoScrollView.ViewportHeight
                - VideoScrollView.VerticalOffset
                <= 240
        )
        {
            _ = ViewModel.LoadVideosAsync();
        }
    }

    private void OnScrollViewSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (e.NewSize.Width > 100)
        {
            CheckVideoCount();
        }
    }

    private void CheckVideoCount()
    {
        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            if (
                VideoScrollView.ScrollableHeight <= 240
                && ViewModel
                    is { IsVideoLoading: false, HasMoreCurrentVideos: true, HasCurrentVideos: true }
            )
            {
                _ = ViewModel.LoadVideosAsync();
            }
        });
    }

    private void VideoRepeater_ElementPrepared(
        ItemsRepeater sender,
        ItemsRepeaterElementPreparedEventArgs args
    )
    {
        if (args.Element is FrameworkElement element)
        {
            RevealFocusPanel.SetAttachToPanel(element, GridViewRevealFocusPanel);

            // 确保首次排版与从回收池复用就位前关闭动画，防止产生从 (0,0) 或离屏旧位置的飞入滑行动效
            CompositionFactory.DisableStandardReposition(element);
            _pendingRepositionElements.Add(element);
            if (!_isRepositionFlushScheduled)
            {
                _isRepositionFlushScheduled = true;
                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, FlushPendingReposition);
            }
        }
    }

    private void VideoRepeater_ElementClearing(
        ItemsRepeater sender,
        ItemsRepeaterElementClearingEventArgs args
    )
    {
        if (args.Element is FrameworkElement element)
        {
            _pendingRepositionElements.Remove(element);
            CompositionFactory.DisableStandardReposition(element);
            RecommendedVideoItemTemplate.ClearImage(element);
        }
    }

    private void FlushPendingReposition()
    {
        _isRepositionFlushScheduled = false;
        if (_pendingRepositionElements.Count == 0)
        {
            return;
        }

        foreach (FrameworkElement element in _pendingRepositionElements)
        {
            if (element.IsLoaded)
            {
                CompositionFactory.SetStandardReposition(element);
            }
        }

        _pendingRepositionElements.Clear();
    }
}
