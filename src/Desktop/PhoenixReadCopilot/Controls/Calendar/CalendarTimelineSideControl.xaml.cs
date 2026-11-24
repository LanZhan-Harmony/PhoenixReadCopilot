using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.Items;
using PhoenixReadCopilot.ViewModels.View;

namespace PhoenixReadCopilot.Controls.Calendar;

/// <summary>
/// 上新日历左侧时间轴.
/// </summary>
internal sealed partial class CalendarTimelineSideControl : UserControl
{
    public CalendarPageViewModel? ViewModel
    {
        get => (CalendarPageViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(CalendarPageViewModel),
        typeof(CalendarTimelineSideControl),
        new PropertyMetadata(null, (d, e) =>
            ((CalendarTimelineSideControl)d).OnViewModelChanged(
                (CalendarPageViewModel?)e.OldValue,
                (CalendarPageViewModel?)e.NewValue
            )
        )
    );

    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarTimelineSideControl"/> class.
    /// </summary>
    public CalendarTimelineSideControl()
    {
        InitializeComponent();
        Loaded += (_, _) => OnControlLoaded();
        Unloaded += (_, _) => OnControlUnloaded();
    }

    private void OnControlLoaded()
    {
        ViewModel?.TimelineInitialized += OnTimelineInitialized;

        TimelineView.SelectionChanged += OnTimelineSelectionChanged;
        CheckTimelineSelection();
    }

    private void OnControlUnloaded()
    {
        TimelineView.ItemsSource = null;
        TimelineView.SelectionChanged -= OnTimelineSelectionChanged;
        ViewModel?.TimelineInitialized -= OnTimelineInitialized;
    }

    private void OnViewModelChanged(
        CalendarPageViewModel? oldValue,
        CalendarPageViewModel? newValue
    )
    {
        oldValue?.TimelineInitialized -= OnTimelineInitialized;

        if (newValue is not null)
        {
            newValue.TimelineInitialized += OnTimelineInitialized;
            CheckTimelineSelection();
        }
    }

    private void OnTimelineInitialized(object? sender, EventArgs e) => CheckTimelineSelection();

    private void CheckTimelineSelection()
    {
        if (ViewModel?.SelectedTimeline is null)
        {
            return;
        }

        int index = ViewModel.Timelines.IndexOf(ViewModel.SelectedTimeline);
        if (index >= 0)
        {
            TimelineView.Select(index);
        }
    }

    private void OnTimelineSelectionChanged(
        ItemsView sender,
        ItemsViewSelectionChangedEventArgs args
    )
    {
        if (ViewModel is null)
        {
            return;
        }

        if (TimelineView.SelectedItem is TimelineItemViewModel item)
        {
            ViewModel.SelectTimeline(item);
        }
    }
}
