using PhoenixReadCopilot.UI.ViewModels.Core;
using CommunityToolkit.WinUI;
using Richasy.WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.UI.Controls.Core;

/// <summary>
/// 播放器.
/// </summary>
public sealed partial class PlayerPresenter : PlayerPresenterBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerPresenter"/> class.
    /// </summary>
    public PlayerPresenter() => InitializeComponent();

    /// <summary>
    /// 尝试获取岛屿播放器.
    /// </summary>
    /// <returns>实例.</returns>
    public IslandPlayer? TryGetIslandPlayer() => this.FindDescendant<IslandPlayer>();
}

/// <summary>
/// 播放器基类.
/// </summary>
public abstract class PlayerPresenterBase : LayoutUserControlBase<PlayerViewModelBase> { }
