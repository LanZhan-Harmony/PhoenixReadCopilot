using CommunityToolkit.Mvvm.DependencyInjection;
using PhoenixReadCopilot.ViewModels.View;

namespace PhoenixReadCopilot.Pages;

/// <summary>
/// 影院页面.
/// </summary>
internal sealed partial class FilterPage : FilterPageBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FilterPage"/> class.
    /// </summary>
    public FilterPage()
    {
        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override void OnPageLoaded() => _ = ViewModel.InitializeAsync();
}

/// <summary>
/// 影院页面基类.
/// </summary>
internal abstract class FilterPageBase : LayoutPageBase<FilterPageViewModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FilterPageBase"/> class.
    /// </summary>
    protected FilterPageBase()
    {
        ViewModel = Ioc.Default.GetRequiredService<FilterPageViewModel>();
    }
}
