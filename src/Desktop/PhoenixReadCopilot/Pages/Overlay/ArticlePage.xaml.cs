using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Navigation;
using PhoenixReadCopilot.ViewModels.View;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.Pages.Overlay;

/// <summary>
/// 红果圈图文详情浮层页面（模仿小红书左图右文评）.
/// </summary>
internal sealed partial class ArticlePage : ArticlePageBase
{
    public ArticlePage()
    {
        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        if (e.Parameter is not null)
        {
            _ = ViewModel.InitializeAsync(e.Parameter);
        }
    }

    private void OnPlayDramaButtonClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Microsoft.UI.Xaml.Controls.Button { Tag: string dramaId } && !string.IsNullOrEmpty(dramaId))
        {
            ViewModel.PlayDramaCommand.Execute(dramaId);
        }
    }
}

/// <summary>
/// 页面基类.
/// </summary>
internal abstract class ArticlePageBase : LayoutPageBase<ArticlePageViewModel>
{
    protected ArticlePageBase()
    {
        ViewModel = Ioc.Default.GetRequiredService<ArticlePageViewModel>();
    }
}
