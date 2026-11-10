namespace PhoenixReadCopilot.Controls.Pgc;

/// <summary>
/// 上新日历右侧剧集列表.
/// </summary>
public sealed partial class AnimeTimelineMainControl : AnimeTimelineControlBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AnimeTimelineMainControl"/> class.
    /// </summary>
    public AnimeTimelineMainControl() => InitializeComponent();

    protected override void OnControlUnloaded() => SeasonRepeater.ItemsSource = null;
}
