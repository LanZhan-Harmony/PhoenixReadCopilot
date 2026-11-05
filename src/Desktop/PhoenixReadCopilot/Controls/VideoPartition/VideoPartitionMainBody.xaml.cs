using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.Components;
using WinUIKernel.Share;

namespace PhoenixReadCopilot.Controls.VideoPartition;

/// <summary>
/// 视频分区主体.
/// </summary>
public sealed partial class VideoPartitionMainBody : VideoPartitionDetailControlBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VideoPartitionMainBody"/> class.
    /// </summary>
    public VideoPartitionMainBody() => InitializeComponent();

    /// <inheritdoc/>
    protected override void OnControlLoaded()
    {
        VideoScrollView.ViewChanged += OnViewChanged;
        VideoScrollView.SizeChanged += OnScrollViewSizeChanged;
        if (ViewModel is null)
        {
            return;
        }

        CheckVideoCount();
    }

    /// <inheritdoc/>
    protected override void OnControlUnloaded()
    {
        ViewModel?.VideoListUpdated -= OnVideoListUpdatedAsync;

        VideoRepeater.ItemsSource = null;
        VideoScrollView.ViewChanged -= OnViewChanged;
        VideoScrollView.SizeChanged -= OnScrollViewSizeChanged;
    }

    /// <inheritdoc/>
    protected override void OnViewModelChanged(
        VideoPartitionDetailViewModel? oldValue,
        VideoPartitionDetailViewModel? newValue
    )
    {
        oldValue?.VideoListUpdated -= OnVideoListUpdatedAsync;

        if (newValue is null)
        {
            return;
        }

        newValue.VideoListUpdated += OnVideoListUpdatedAsync;
    }

    private async void OnVideoListUpdatedAsync(object? sender, EventArgs e)
    {
        await Task.Delay(500);
        CheckVideoCount();
    }

    private void OnViewChanged(object? sender, ScrollViewerViewChangedEventArgs args)
    {
        WinUIKernelShareExtensions.IsCardAnimationEnabled = !args.IsIntermediate;
        DispatcherQueue.TryEnqueue(
            DispatcherQueuePriority.Low,
            () =>
            {
                if (
                    VideoScrollView.ExtentHeight
                        - VideoScrollView.ViewportHeight
                        - VideoScrollView.VerticalOffset
                    <= 240
                )
                {
                    ViewModel.LoadVideosCommand.Execute(default);
                }
            }
        );
    }

    private void OnScrollViewSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (e.NewSize.Width > 100 && ViewModel is not null)
        {
            CheckVideoCount();
        }
    }

    private void CheckVideoCount()
    {
        DispatcherQueue.TryEnqueue(
            DispatcherQueuePriority.Low,
            () =>
            {
                if (VideoScrollView.ScrollableHeight <= 240 && ViewModel is not null)
                {
                    ViewModel.LoadVideosCommand.Execute(default);
                }
            }
        );
    }
}
