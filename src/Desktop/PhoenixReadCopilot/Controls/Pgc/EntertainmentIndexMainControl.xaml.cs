using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.View;
using WinUIKernel.Share;

namespace PhoenixReadCopilot.Controls.Pgc;

/// <summary>
/// 娱乐索引主体.
/// </summary>
public sealed partial class EntertainmentIndexMainControl : EntertainmentIndexControlBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EntertainmentIndexMainControl"/> class.
    /// </summary>
    public EntertainmentIndexMainControl() => InitializeComponent();

    /// <inheritdoc/>
    protected override void OnControlLoaded()
    {
        SeasonScrollView.ViewChanged += OnViewChanged;
        SeasonScrollView.SizeChanged += OnScrollViewSizeChanged;

        if (ViewModel is null)
        {
            return;
        }

        CheckSeasonCount();
    }

    /// <inheritdoc/>
    protected override void OnControlUnloaded()
    {
        ViewModel?.ItemsUpdated -= OnItemsUpdatedAsync;

        SeasonRepeater.ItemsSource = null;
        SeasonScrollView.ViewChanged -= OnViewChanged;
        SeasonScrollView.SizeChanged -= OnScrollViewSizeChanged;
    }

    /// <inheritdoc/>
    protected override void OnViewModelChanged(
        CinemaPageViewModel? oldValue,
        CinemaPageViewModel? newValue
    )
    {
        oldValue?.ItemsUpdated -= OnItemsUpdatedAsync;

        if (newValue is null)
        {
            return;
        }

        SeasonScrollView?.ChangeView(0, 0, null, true);
        newValue.ItemsUpdated += OnItemsUpdatedAsync;
    }

    private async void OnItemsUpdatedAsync(object? sender, EventArgs e)
    {
        await Task.Delay(500);
        CheckSeasonCount();
    }

    private void OnViewChanged(object? sender, ScrollViewerViewChangedEventArgs args)
    {
        WinUIKernelShareExtensions.IsCardAnimationEnabled = !args.IsIntermediate;
        DispatcherQueue.TryEnqueue(
            DispatcherQueuePriority.Low,
            () =>
            {
                if (
                    SeasonScrollView.ExtentHeight
                    - SeasonScrollView.ViewportHeight
                    - SeasonScrollView.VerticalOffset
                    <= 240
                )
                {
                    ViewModel.RequestIndexCommand.Execute(null);
                }
            }
        );
    }

    private void OnScrollViewSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (e.NewSize.Width > 100)
        {
            CheckSeasonCount();
        }
    }

    private void CheckSeasonCount()
    {
        DispatcherQueue.TryEnqueue(
            DispatcherQueuePriority.Low,
            () =>
            {
                if (SeasonScrollView.ScrollableHeight <= 240 && ViewModel is not null)
                {
                    ViewModel.RequestIndexCommand.Execute(null);
                }
            }
        );
    }
}
