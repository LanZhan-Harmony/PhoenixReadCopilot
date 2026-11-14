using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.Controls.Components;

/// <summary>
/// 上新日历视频卡片展示.
/// </summary>
public sealed partial class CalendarVideoPresenter : VideoCardPresenter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarVideoPresenter"/> class.
    /// </summary>
    public CalendarVideoPresenter()
    {
        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override void OnViewModelChanged(
        VideoItemViewModel? oldValue,
        VideoItemViewModel? newValue
    )
    {
        base.OnViewModelChanged(oldValue, newValue);
        Bindings.Update();
    }
}
