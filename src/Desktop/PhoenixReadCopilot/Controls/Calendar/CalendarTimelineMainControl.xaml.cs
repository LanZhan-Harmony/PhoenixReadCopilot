namespace PhoenixReadCopilot.Controls.Calendar;

/// <summary>
/// 上新日历右侧剧集列表.
/// </summary>
public sealed partial class CalendarTimelineMainControl : CalendarTimelineControlBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarTimelineMainControl"/> class.
    /// </summary>
    public CalendarTimelineMainControl() => InitializeComponent();

    protected override void OnControlUnloaded() => SeasonRepeater.ItemsSource = null;
}
