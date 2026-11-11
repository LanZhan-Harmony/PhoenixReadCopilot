using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.Components;
using WinUIKernel.Share;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.Controls.Moment;

/// <summary>
/// 视频动态详情控件.
/// </summary>
public sealed partial class VideoMomentSectionDetailControl : VideoMomentSectionDetailControlBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VideoMomentSectionDetailControl"/> class.
    /// </summary>
    public VideoMomentSectionDetailControl() => InitializeComponent();

    /// <inheritdoc/>
    protected override void OnControlLoaded()
    {
        ViewModel.ListUpdated += OnListUpdatedAsync;
        VideoScrollView.ViewChanged += OnViewChanged;
        VideoScrollView.SizeChanged += OnScrollViewSizeChanged;

        CheckVideoCount();
    }

    /// <inheritdoc/>
    protected override void OnControlUnloaded()
    {
        MomentRepeater.ItemsSource = null;
        ViewModel.ListUpdated -= OnListUpdatedAsync;
        VideoScrollView.ViewChanged -= OnViewChanged;
        VideoScrollView.SizeChanged -= OnScrollViewSizeChanged;
    }

    private async void OnListUpdatedAsync(object? sender, EventArgs e)
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
            )
            {
                ViewModel.LoadItemsCommand.Execute(null);
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
            if (VideoScrollView.ScrollableHeight <= 240 && ViewModel is not null)
            {
                ViewModel.LoadItemsCommand.Execute(null);
            }
        });
    }
}

/// <summary>
/// 视频动态详情控件基类.
/// </summary>
public abstract class VideoMomentSectionDetailControlBase
    : LayoutUserControlBase<VideoMomentSectionDetailViewModel> { }
