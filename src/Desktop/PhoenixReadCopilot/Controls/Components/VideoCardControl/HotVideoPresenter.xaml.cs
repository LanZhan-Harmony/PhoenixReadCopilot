using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.Controls.Components;

/// <summary>
/// 热门视频卡片展示.
/// </summary>
public sealed partial class HotVideoPresenter : VideoCardPresenter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HotVideoPresenter"/> class.
    /// </summary>
    public HotVideoPresenter() => InitializeComponent();

    /// <inheritdoc/>
    protected override void OnViewModelChanged(VideoItemViewModel? oldValue, VideoItemViewModel? newValue)
    {
        base.OnViewModelChanged(oldValue, newValue);
        Bindings.Update();
    }
}
