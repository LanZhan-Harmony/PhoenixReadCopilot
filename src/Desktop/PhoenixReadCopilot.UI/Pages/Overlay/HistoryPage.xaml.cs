using PhoenixReadCopilot.UI.ViewModels.View;
using Richasy.WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.UI.Pages.Overlay;

/// <summary>
/// 历史记录页面.
/// </summary>
public sealed partial class HistoryPage : HistoryPageBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HistoryPage"/> class.
    /// </summary>
    public HistoryPage() => InitializeComponent();

    /// <inheritdoc/>
    protected override void OnPageLoaded() => ViewModel.InitializeCommand.Execute(default);
}

/// <summary>
/// 历史记录页面基类.
/// </summary>
public abstract class HistoryPageBase : LayoutPageBase<HistoryPageViewModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HistoryPageBase"/> class.
    /// </summary>
    protected HistoryPageBase() => ViewModel = this.Get<HistoryPageViewModel>();
}
