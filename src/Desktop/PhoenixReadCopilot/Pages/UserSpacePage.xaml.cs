using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using PhoenixReadCopilot.ViewModels.View;

namespace PhoenixReadCopilot.Pages;

/// <summary>
/// 用户空间/个人主页页面（支持个人与他人共用）.
/// </summary>
internal sealed partial class UserSpacePage : Page
{
    /// <summary>
    /// 视图模型.
    /// </summary>
    public UserSpacePageViewModel ViewModel { get; } = Ioc.Default.GetRequiredService<UserSpacePageViewModel>();

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

    /// <inheritdoc/>
    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        ViewModel.Uninitialize();
    }
}
