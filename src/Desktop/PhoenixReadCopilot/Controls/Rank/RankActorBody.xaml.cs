using System.ComponentModel;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.View;
using WinUI3Package;

namespace PhoenixReadCopilot.Controls.Rank;

/// <summary>
/// 演员榜单展示主体.
/// </summary>
internal sealed partial class RankActorBody : RankPageControlBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RankActorBody"/> class.
    /// </summary>
    public RankActorBody()
    {
        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override void OnViewModelChanged(
        RankPageViewModel? oldValue,
        RankPageViewModel? newValue
    )
    {
        base.OnViewModelChanged(oldValue, newValue);
        if (oldValue is not null)
        {
            oldValue.VideoListUpdated -= OnActorListUpdatedAsync;
            oldValue.PropertyChanged -= OnViewModelPropertyChanged;
        }

        if (newValue is not null && IsLoaded)
        {
            newValue.VideoListUpdated += OnActorListUpdatedAsync;
            newValue.PropertyChanged += OnViewModelPropertyChanged;
            CheckActorCount();
        }
    }

    /// <inheritdoc/>
    protected override void OnControlLoaded()
    {
        if (ViewModel is not null)
        {
            ViewModel.VideoListUpdated += OnActorListUpdatedAsync;
            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        ActorScrollView.ViewChanged += OnViewChanged;
        ActorScrollView.SizeChanged += OnScrollViewSizeChanged;

        CheckActorCount();
    }

    /// <inheritdoc/>
    protected override void OnControlUnloaded()
    {
        ActorRepeater.ItemsSource = null;
        if (ViewModel is not null)
        {
            ViewModel.VideoListUpdated -= OnActorListUpdatedAsync;
            ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        ActorScrollView.ViewChanged -= OnViewChanged;
        ActorScrollView.SizeChanged -= OnScrollViewSizeChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.SelectedSection))
        {
            ActorScrollView.ChangeView(null, 0, null, true);
        }
    }

    private async void OnActorListUpdatedAsync(object? sender, EventArgs e)
    {
        await Task.Delay(500);
        CheckActorCount();
    }

    private void OnViewChanged(object? sender, ScrollViewerViewChangedEventArgs args)
    {
        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            if (
                ActorScrollView.ExtentHeight
                    - ActorScrollView.ViewportHeight
                    - ActorScrollView.VerticalOffset
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
            CheckActorCount();
        }
    }

    private void CheckActorCount()
    {
        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            if (
                ActorScrollView.ScrollableHeight <= 240
                && ViewModel is { IsVideoLoading: false }
                && (ViewModel?.Actors.Count ?? 0) > 0
            )
            {
                _ = ViewModel.LoadVideosAsync();
            }
        });
    }

    private void ActorRepeater_ElementPrepared(
        ItemsRepeater sender,
        ItemsRepeaterElementPreparedEventArgs args
    )
    {
        if (args.Element is FrameworkElement element)
        {
            RevealFocusPanel.SetAttachToPanel(element, ListViewRevealFocusPanel);
        }
    }
}
