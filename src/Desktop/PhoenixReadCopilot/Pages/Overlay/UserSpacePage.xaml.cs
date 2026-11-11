using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Navigation;
using PhoenixReadCopilot.ViewModels.View;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.Pages.Overlay;

/// <summary>
/// 用户空间页面.
/// </summary>
public sealed partial class UserSpacePage : UserSpacePageBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserSpacePage"/> class.
    /// </summary>
    public UserSpacePage()
    {
        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        if (e.Parameter is not null)
        {
            ViewModel.InitializeCommand.Execute(e.Parameter);
        }
    }
}

/// <summary>
/// 用户空间页面基类.
/// </summary>
public abstract class UserSpacePageBase : LayoutPageBase<UserSpacePageViewModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserSpacePageBase"/> class.
    /// </summary>
    protected UserSpacePageBase()
    {
        ViewModel = Ioc.Default.GetRequiredService<UserSpacePageViewModel>();
    }
}
