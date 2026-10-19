using PhoenixReadCopilot.UI.ViewModels.View;
using Richasy.WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.UI.Controls.Search;

/// <summary>
/// 搜索页面控件基类.
/// </summary>
public abstract class SearchPageControlBase : LayoutUserControlBase<SearchPageViewModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SearchPageControlBase"/> class.
    /// </summary>
    protected SearchPageControlBase() => ViewModel = this.Get<SearchPageViewModel>();
}
