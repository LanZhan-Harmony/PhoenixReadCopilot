using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.Controls.Components;

/// <summary>
/// 筛选视频卡片展示.
/// </summary>
public sealed partial class FilterVideoPresenter : VideoCardPresenter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FilterVideoPresenter"/> class.
    /// </summary>
    public FilterVideoPresenter()
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
