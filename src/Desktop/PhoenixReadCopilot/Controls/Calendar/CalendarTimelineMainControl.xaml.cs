using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.Helpers.Animations;
using PhoenixReadCopilot.ViewModels.View;
using WinUI3Package;

namespace PhoenixReadCopilot.Controls.Calendar;

/// <summary>
/// 上新日历右侧剧集列表.
/// </summary>
internal sealed partial class CalendarTimelineMainControl : UserControl
{
    public CalendarPageViewModel? ViewModel
    {
        get => (CalendarPageViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(CalendarPageViewModel),
        typeof(CalendarTimelineMainControl),
        new PropertyMetadata(null, (d, e) =>
            ((CalendarTimelineMainControl)d).OnViewModelChanged(
                (CalendarPageViewModel?)e.OldValue,
                (CalendarPageViewModel?)e.NewValue
            )
        )
    );

    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarTimelineMainControl"/> class.
    /// </summary>
    public CalendarTimelineMainControl()
    {
        InitializeComponent();
        Loaded += (_, _) => OnControlLoaded();
        Unloaded += (_, _) => OnControlUnloaded();
    }

    private void OnControlLoaded()
    {
        TimelineScrollView.ViewChanged += OnViewChanged;
        TimelineScrollView.SizeChanged += OnScrollViewSizeChanged;

        if (ViewModel is null)
        {
            return;
        }

        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        ViewModel.TimelineItemsUpdated += OnTimelineItemsUpdatedAsync;

        CheckTimelineCount();
    }

    private void OnControlUnloaded()
    {
        if (ViewModel is not null)
        {
            ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            ViewModel.TimelineItemsUpdated -= OnTimelineItemsUpdatedAsync;
        }

        TimelineRepeater.ItemsSource = null;
        TimelineScrollView.ViewChanged -= OnViewChanged;
        TimelineScrollView.SizeChanged -= OnScrollViewSizeChanged;
    }

    private void OnViewModelChanged(
        CalendarPageViewModel? oldValue,
        CalendarPageViewModel? newValue
    )
    {
        if (oldValue is not null)
        {
            oldValue.PropertyChanged -= OnViewModelPropertyChanged;
            oldValue.TimelineItemsUpdated -= OnTimelineItemsUpdatedAsync;
        }

        if (newValue is null)
        {
            return;
        }

        TimelineScrollView?.ChangeView(0, 0, null, true);
        if (IsLoaded)
        {
            newValue.PropertyChanged += OnViewModelPropertyChanged;
            newValue.TimelineItemsUpdated += OnTimelineItemsUpdatedAsync;
            CheckTimelineCount();
        }
    }

    private void OnViewModelPropertyChanged(
        object? sender,
        System.ComponentModel.PropertyChangedEventArgs e
    )
    {
        if (e.PropertyName == nameof(ViewModel.SelectedTimeline))
        {
            TimelineScrollView?.ChangeView(0, 0, null, true);
            CheckTimelineCount();
        }
    }

    private async void OnTimelineItemsUpdatedAsync(object? sender, EventArgs e)
    {
        await Task.Delay(500);
        CheckTimelineCount();
    }

    private void OnViewChanged(object? sender, ScrollViewerViewChangedEventArgs args)
    {
        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            if (
                TimelineScrollView.ExtentHeight
                    - TimelineScrollView.ViewportHeight
                    - TimelineScrollView.VerticalOffset
                <= 240
            )
            {
                _ = ViewModel?.RequestMoreDateReleasesAsync();
            }
        });
    }

    private void OnScrollViewSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (e.NewSize.Width > 100)
        {
            CheckTimelineCount();
        }
    }

    private void CheckTimelineCount()
    {
        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            if (TimelineScrollView.ScrollableHeight <= 240 && ViewModel is not null)
            {
                _ = ViewModel.RequestMoreDateReleasesAsync();
            }
        });
    }

    private void TimelineRepeater_ElementPrepared(
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

    private void TimelineRepeater_ElementClearing(
        ItemsRepeater sender,
        ItemsRepeaterElementClearingEventArgs args
    )
    {
        if (args.Element is FrameworkElement element)
        {
            CompositionFactory.DisableStandardReposition(element);
            CalendarVideoItemTemplate.ClearImage(element);
        }
    }
}
