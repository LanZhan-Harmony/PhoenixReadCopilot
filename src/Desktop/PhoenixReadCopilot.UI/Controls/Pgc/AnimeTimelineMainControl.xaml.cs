namespace PhoenixReadCopilot.UI.Controls.Pgc;

/// <summary>
/// 动漫时间轴主控件.
/// </summary>
public sealed partial class AnimeTimelineMainControl : AnimeTimelineControlBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AnimeTimelineMainControl"/> class.
    /// </summary>
    public AnimeTimelineMainControl() => InitializeComponent();

    protected override void OnControlUnloaded() => SeasonRepeater.ItemsSource = default;
}
