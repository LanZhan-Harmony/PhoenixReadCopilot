namespace PhoenixReadCopilot.Controls.FindDrama;

/// <summary>
/// 娱乐索引侧边栏控件.
/// </summary>
public sealed partial class FindDramaIndexSideControl : FindDramaIndexControlBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FindDramaIndexSideControl"/> class.
    /// </summary>
    public FindDramaIndexSideControl() => InitializeComponent();

    protected override void OnControlUnloaded() => FilterRepeater.ItemsSource = null;
}
