using CommunityToolkit.Mvvm.DependencyInjection;
using PhoenixReadCopilot.ViewModels.View;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.Pages;

/// <summary>
/// 影院页面.
/// </summary>
public partial class FilterPage : FilterPageBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FilterPage"/> class.
    /// </summary>
    public FilterPage()
    {
        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override void OnPageLoaded() => ViewModel.InitializeCommand.Execute(null);
}

/// <summary>
/// 影院页面基类.
/// </summary>
public abstract class FilterPageBase : LayoutPageBase<FilterPageViewModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FilterPageBase"/> class.
    /// </summary>
    protected FilterPageBase()
    {
        ViewModel = Ioc.Default.GetRequiredService<FilterPageViewModel>();
    }
}
