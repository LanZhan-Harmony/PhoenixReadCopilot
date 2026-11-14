using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUIKernel.Share;

namespace PhoenixReadCopilot.Controls.Popular;

/// <summary>
/// 流行与榜单页面主体.
/// </summary>
public sealed partial class PopularMainBody : PopularPageControlBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PopularMainBody"/> class.
    /// </summary>
    public PopularMainBody()
    {
        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override void OnControlLoaded()
    {
        ViewModel.VideoListUpdated += OnVideoListUpdatedAsync;
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        VideoScrollView.ViewChanged += OnViewChanged;
        VideoScrollView.SizeChanged += OnScrollViewSizeChanged;

        CheckVideoCount();
    }

    /// <inheritdoc/>
    protected override void OnControlUnloaded()
    {
        DramaRepeater.ItemsSource = null;
        ActorRepeater.ItemsSource = null;
        ViewModel.VideoListUpdated -= OnVideoListUpdatedAsync;
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        VideoScrollView.ViewChanged -= OnViewChanged;
        VideoScrollView.SizeChanged -= OnScrollViewSizeChanged;
    }

    private void OnViewModelPropertyChanged(
        object? sender,
        System.ComponentModel.PropertyChangedEventArgs e
    )
    {
        if (
            e.PropertyName == nameof(ViewModel.SelectedSection)
            || e.PropertyName == nameof(ViewModel.CurrentChannel)
        )
        {
            VideoScrollView.ChangeView(null, 0, null, true);
        }
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
            bool hasItems = ViewModel?.IsActorLeaderboard == true
                ? (ViewModel?.Actors.Count ?? 0) > 0
                : (ViewModel?.Videos.Count ?? 0) > 0;

            if (
                VideoScrollView.ScrollableHeight <= 240
                && ViewModel is { IsVideoLoading: false }
                && hasItems
            )
            {
                ViewModel.LoadVideosCommand.Execute(null);
            }
        });
    }
}
