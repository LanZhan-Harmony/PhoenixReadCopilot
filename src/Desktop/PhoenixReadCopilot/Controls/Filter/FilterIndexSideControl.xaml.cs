namespace PhoenixReadCopilot.Controls.Filter;

/// <summary>
/// 娱乐索引侧边栏控件.
/// </summary>
public sealed partial class FilterIndexSideControl : FilterIndexControlBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FilterIndexSideControl"/> class.
    /// </summary>
    public FilterIndexSideControl()
    {
        InitializeComponent();
    }

    protected override void OnControlUnloaded() => FilterRepeater.ItemsSource = null;
}
