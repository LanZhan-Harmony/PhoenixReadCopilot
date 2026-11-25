using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using PhoenixReadCopilot.ViewModels.View;

namespace PhoenixReadCopilot.Pages.Overlay;

/// <summary>
/// 搜索页面.
/// </summary>
internal sealed partial class SearchPage : Page
{
    private string _keyword = string.Empty;

    /// <summary>
    /// 视图模型.
    /// </summary>
    public SearchPageViewModel ViewModel { get; } = Ioc.Default.GetRequiredService<SearchPageViewModel>();

    public SearchPage()
    {
        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        if (e.NavigationMode == NavigationMode.Back)
        {
            return;
        }

        if (e.Parameter is string keyword)
        {
            _keyword = keyword;
        }
    }

    /// <inheritdoc/>
    protected override void OnNavigatedFrom(NavigationEventArgs e) => _keyword = string.Empty;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_keyword))
        {
            ViewModel.Search(_keyword);
        }
    }
}
