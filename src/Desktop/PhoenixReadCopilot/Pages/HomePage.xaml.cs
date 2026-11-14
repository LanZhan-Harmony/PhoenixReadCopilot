using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.View;
using WinUIKernel.Share;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.Pages;

/// <summary>
/// 首页（推荐短剧）页面.
/// </summary>
public sealed partial class HomePage : HomePageBase
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
        ViewModel.InitializeCommand.Execute(null);
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
    }

    private async void OnVideoListUpdatedAsync(object? sender, EventArgs e)
    {
        await Task.Delay(500);
        CheckVideoCount();
    }

    private void OnViewChanged(object? sender, ScrollViewerViewChangedEventArgs args)
    {
        WinUIKernelShareExtensions.IsCardAnimationEnabled = !args.IsIntermediate;

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
                ViewModel.LoadVideosCommand.Execute(null);
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
                ViewModel.LoadVideosCommand.Execute(null);
            }
        });
    }
}

/// <summary>
/// 首页基类.
/// </summary>
public abstract class HomePageBase : LayoutPageBase<HomePageViewModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HomePageBase"/> class.
    /// </summary>
    protected HomePageBase()
    {
        ViewModel = Ioc.Default.GetRequiredService<HomePageViewModel>();
    }
}
