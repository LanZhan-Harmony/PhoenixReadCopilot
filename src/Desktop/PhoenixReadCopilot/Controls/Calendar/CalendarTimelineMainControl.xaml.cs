using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.Controls.Home;
using WinUI3Package;

namespace PhoenixReadCopilot.Controls.Calendar;

/// <summary>
/// 上新日历右侧剧集列表.
/// </summary>
public sealed partial class CalendarTimelineMainControl : CalendarTimelineControlBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarTimelineMainControl"/> class.
    /// </summary>
    public CalendarTimelineMainControl()
    {
        InitializeComponent();
    }

    protected override void OnControlUnloaded() => TimelineRepeater.ItemsSource = null;

    private void TimelineRepeater_ElementPrepared(
        ItemsRepeater sender,
        ItemsRepeaterElementPreparedEventArgs args
    )
    {
        if (args.Element is FrameworkElement element)
        {
            RevealFocusPanel.SetAttachToPanel(element, GridViewRevealFocusPanel);
        }
    }

    private void TimelineRepeater_ElementClearing(
        ItemsRepeater sender,
        ItemsRepeaterElementClearingEventArgs args
    )
    {
        if (args.Element is FrameworkElement element)
        {
            CalendarVideoItemTemplate.ClearImage(element);
        }
    }
}
