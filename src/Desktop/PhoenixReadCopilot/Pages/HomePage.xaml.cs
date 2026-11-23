using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.Controls.Home;
using PhoenixReadCopilot.Helpers.Animations;
using PhoenixReadCopilot.ViewModels.View;
using WinUI3Package;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.Pages;

/// <summary>
/// 首页（推荐短剧）页面.
/// </summary>
internal sealed partial class HomePage : HomePageBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HomePage"/> class.
    /// </summary>
    public HomePage()
    {
        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override void OnPageLoaded()
    {
        _ = ViewModel.InitializeAsync();
        ViewModel.VideoListUpdated += OnVideoListUpdatedAsync;
        VideoScrollView.ViewChanged += OnViewChanged;
        VideoScrollView.SizeChanged += OnScrollViewSizeChanged;
        CheckVideoCount();
    }

    /// <inheritdoc/>
    protected override void OnPageUnloaded()
    {
        VideoRepeater.ItemsSource = null;
        ViewModel.VideoListUpdated -= OnVideoListUpdatedAsync;
        VideoScrollView.ViewChanged -= OnViewChanged;
        VideoScrollView.SizeChanged -= OnScrollViewSizeChanged;
        ViewModel.Videos.Clear();
    }

    private async void OnVideoListUpdatedAsync(object? sender, EventArgs e)
    {
        await Task.Delay(500);
        CheckVideoCount();
    }

    private void OnViewChanged(object? sender, ScrollViewerViewChangedEventArgs args)
    {
        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            if (
                VideoScrollView.ExtentHeight
                    - VideoScrollView.ViewportHeight
                    - VideoScrollView.VerticalOffset
                    <= 240
                && ViewModel is { IsVideoLoading: false }
            )
            {
                _ = ViewModel.LoadVideosAsync();
            }
        });
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
                && ViewModel is { IsVideoLoading: false, Videos.Count: > 0 }
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

            // 待当前帧排版完成（Visual.Offset 坐标就位）后，再挂载平滑重排动画
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                CompositionFactory.SetStandardReposition(element);
            });
        }
    }

    private void VideoRepeater_ElementClearing(
        ItemsRepeater sender,
        ItemsRepeaterElementClearingEventArgs args
    )
    {
        if (args.Element is FrameworkElement element)
        {
            CompositionFactory.DisableStandardReposition(element);
            RecommendedVideoItemTemplate.ClearImage(element);
        }
    }
}

/// <summary>
/// 首页基类.
/// </summary>
internal abstract class HomePageBase : LayoutPageBase<HomePageViewModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HomePageBase"/> class.
    /// </summary>
    protected HomePageBase()
    {
        ViewModel = Ioc.Default.GetRequiredService<HomePageViewModel>();
    }
}
