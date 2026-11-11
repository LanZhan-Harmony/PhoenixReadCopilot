using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.Items;
using PhoenixReadCopilot.ViewModels.View;

namespace PhoenixReadCopilot.Controls.Calendar;

/// <summary>
/// 上新日历左侧时间轴.
/// </summary>
public sealed partial class CalendarTimelineSideControl : CalendarTimelineControlBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarTimelineSideControl"/> class.
    /// </summary>
    public CalendarTimelineSideControl()
    {
        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override void OnControlLoaded()
    {
        ViewModel?.TimelineInitialized += OnTimelineInitialized;

        TimelineView.SelectionChanged += OnTimelineSelectionChanged;
        CheckTimelineSelection();
    }

    /// <inheritdoc/>
    protected override void OnControlUnloaded()
    {
        TimelineView.ItemsSource = null;
        TimelineView.SelectionChanged -= OnTimelineSelectionChanged;
        ViewModel?.TimelineInitialized -= OnTimelineInitialized;
    }

    /// <inheritdoc/>
    protected override void OnViewModelChanged(
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
            ViewModel.SelectTimelineCommand.Execute(item);
        }
    }
}
