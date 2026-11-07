using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.Controls.Components;

/// <summary>
/// 推荐视频卡片展示.
/// </summary>
public sealed partial class RecommendVideoPresenter : VideoCardPresenter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RecommendVideoPresenter"/> class.
    /// </summary>
    public RecommendVideoPresenter() => InitializeComponent();

    /// <inheritdoc/>
    protected override void OnViewModelChanged(VideoItemViewModel? oldValue, VideoItemViewModel? newValue)
    {
        base.OnViewModelChanged(oldValue, newValue);
        Bindings.Update();
    }
}
