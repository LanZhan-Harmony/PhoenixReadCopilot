using CommunityToolkit.Mvvm.DependencyInjection;
using PhoenixReadCopilot.ViewModels.View;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.Pages;

/// <summary>
/// 影院页面.
/// </summary>
public partial class FindDramaPage : FindDramaPageBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FindDramaPage"/> class.
    /// </summary>
    public FindDramaPage()
    {
        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override void OnPageLoaded() => ViewModel.InitializeCommand.Execute(null);
}

/// <summary>
/// 影院页面基类.
/// </summary>
public abstract class FindDramaPageBase : LayoutPageBase<FindDramaPageViewModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FindDramaPageBase"/> class.
    /// </summary>
    protected FindDramaPageBase()
    {
        ViewModel = Ioc.Default.GetRequiredService<FindDramaPageViewModel>();
    }
}
