using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.Controls.Components;

/// <summary>
/// 排行榜视频卡片展示.
/// </summary>
public sealed partial class RankVideoPresenter : VideoCardPresenter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RankVideoPresenter"/> class.
    /// </summary>
    public RankVideoPresenter()
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
