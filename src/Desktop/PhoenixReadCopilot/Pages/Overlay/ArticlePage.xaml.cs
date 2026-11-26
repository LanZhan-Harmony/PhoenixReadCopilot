using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using PhoenixReadApi;
using PhoenixReadCopilot.ViewModels.View;

namespace PhoenixReadCopilot.Pages.Overlay;

/// <summary>
/// 红果圈图文详情浮层页面（模仿小红书左图右文评）.
/// </summary>
internal sealed partial class ArticlePage : Page
{
    /// <summary>
    /// 视图模型.
    /// </summary>
    public ArticlePageViewModel ViewModel { get; } = Ioc.Default.GetRequiredService<ArticlePageViewModel>();

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
        if (sender is Microsoft.UI.Xaml.Controls.Button { Tag: Drama drama } && !string.IsNullOrEmpty(drama.Id))
        {
            ViewModel.PlayDramaCommand.Execute(drama);
        }
    }
}
