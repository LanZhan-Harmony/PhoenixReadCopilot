using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Navigation;
using PhoenixReadCopilot.ViewModels.View;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.Pages;

/// <summary>
/// 用户空间/个人主页页面（支持个人与他人共用）.
/// </summary>
public sealed partial class UserSpacePage : UserSpacePageBase
{
    public UserSpacePage()
    {
        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _ = ViewModel.InitializeAsync(e.Parameter);
    }
}

/// <summary>
/// 用户空间页面基类.
/// </summary>
public abstract class UserSpacePageBase : LayoutPageBase<UserSpacePageViewModel>
{
    protected UserSpacePageBase()
    {
        ViewModel = Ioc.Default.GetRequiredService<UserSpacePageViewModel>();
    }
}
