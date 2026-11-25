using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.Helpers.Animations;
using PhoenixReadCopilot.ViewModels.View;
using WinUI3Package;

namespace PhoenixReadCopilot.Controls.Rank;

/// <summary>
/// 短剧榜单展示主体.
/// </summary>
internal sealed partial class RankDramaBody : UserControl
{
    public RankPageViewModel? ViewModel
    {
        get => (RankPageViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(RankPageViewModel),
        typeof(RankDramaBody),
        new PropertyMetadata(null, (d, e) =>
            ((RankDramaBody)d).OnViewModelChanged(
                (RankPageViewModel?)e.OldValue,
                (RankPageViewModel?)e.NewValue
            )
        )
    );

    public RankDramaBody()
    {
        InitializeComponent();
    }

    private void OnViewModelChanged(RankPageViewModel? oldValue, RankPageViewModel? newValue)
    {
        if (oldValue is not null)
        {
            oldValue.VideoListUpdated -= OnVideoListUpdatedAsync;
            oldValue.PropertyChanged -= OnViewModelPropertyChanged;
        }

        if (newValue is not null && IsLoaded)
        {
            newValue.VideoListUpdated += OnVideoListUpdatedAsync;
            newValue.PropertyChanged += OnViewModelPropertyChanged;
            CheckVideoCount();
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not null)
        {
            ViewModel.VideoListUpdated += OnVideoListUpdatedAsync;
            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        CheckVideoCount();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not null)
        {
            ViewModel.VideoListUpdated -= OnVideoListUpdatedAsync;
            ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }
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
                && ViewModel is { IsVideoLoading: false }
                && (ViewModel?.Videos.Count ?? 0) > 0
            )
            {
                _ = ViewModel.LoadVideosAsync();
            }
        });
    }

    private void DramaRepeater_ElementPrepared(
        ItemsRepeater sender,
        ItemsRepeaterElementPreparedEventArgs args
    )
    {
        if (args.Element is FrameworkElement element)
        {
            RevealFocusPanel.SetAttachToPanel(element, GridViewRevealFocusPanel);

            CompositionFactory.DisableStandardReposition(element);
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                CompositionFactory.SetStandardReposition(element);
            });
        }
    }

    private void DramaRepeater_ElementClearing(
        ItemsRepeater sender,
        ItemsRepeaterElementClearingEventArgs args
    )
    {
        if (args.Element is FrameworkElement element)
        {
            CompositionFactory.DisableStandardReposition(element);
            RankVideoItemTemplate.ClearImage(element);
        }
    }
}
