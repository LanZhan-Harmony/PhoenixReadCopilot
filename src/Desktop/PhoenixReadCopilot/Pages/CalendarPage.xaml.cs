using CommunityToolkit.Mvvm.DependencyInjection;
using PhoenixReadCopilot.ViewModels.View;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.Pages;

/// <summary>
/// 上新日历页面.
/// </summary>
internal sealed partial class CalendarPage : CalendarPageBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarPage"/> class.
    /// </summary>
    public CalendarPage()
    {
        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override void OnPageLoaded() => _ = ViewModel.InitializeAsync();
}

/// <summary>
/// 上新日历页面基类.
/// </summary>
internal abstract class CalendarPageBase : LayoutPageBase<CalendarPageViewModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarPageBase"/> class.
    /// </summary>
    protected CalendarPageBase()
    {
        ViewModel = Ioc.Default.GetRequiredService<CalendarPageViewModel>();
    }
}
