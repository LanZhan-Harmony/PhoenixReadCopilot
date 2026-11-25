using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.View;

namespace PhoenixReadCopilot.Pages;

/// <summary>
/// 上新日历页面.
/// </summary>
internal sealed partial class CalendarPage : Page
{
    /// <summary>
    /// 视图模型.
    /// </summary>
    public CalendarPageViewModel ViewModel { get; } = Ioc.Default.GetRequiredService<CalendarPageViewModel>();

    public CalendarPage()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e) => _ = ViewModel.InitializeAsync();
}
