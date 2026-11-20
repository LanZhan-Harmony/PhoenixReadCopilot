using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Pages;
using PhoenixReadCopilot.ViewModels.Core;
using PhoenixReadCopilot.ViewModels.View;
using WinUIEx;

namespace PhoenixReadCopilot.Forms;

/// <summary>
/// 初始窗口.
/// </summary>
public sealed partial class StartupWindow : WindowEx
{
    private readonly AppViewModel _appViewModel = Ioc.Default.GetRequiredService<AppViewModel>();
    private readonly StartupPageViewModel _startupViewModel =
        Ioc.Default.GetRequiredService<StartupPageViewModel>();
    private readonly IAuthenticationService _authService =
        Ioc.Default.GetRequiredService<IAuthenticationService>();

    /// <summary>
    /// Initializes a new instance of the <see cref="StartupWindow"/> class.
    /// </summary>
    public StartupWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        IsMaximizable = false;
        IsMinimizable = false;
        IsResizable = false;
        AppWindow.SetPresenter(AppWindowPresenterKind.CompactOverlay);

        Width = 720;
        Height = 460;

        Title = Strings.Resources.AppName;

        this.CenterOnScreen();
        AppWindow.SetIcon("Assets/Logo/Icon.ico");
        SetTitleBar(TitleBar);
        _appViewModel.Windows.Add(this);

        RootFrame.Navigate(typeof(StartupPage), null, new SuppressNavigationTransitionInfo());

        Closed += OnClosed;
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        _appViewModel.Windows.Remove(this);
        Closed -= OnClosed;

        bool isAuthenticated = false;
        try
        {
            isAuthenticated = _authService.IsAuthenticated;
        }
        catch
        {
            // Ignore
        }

        if (!isAuthenticated)
        {
            _startupViewModel.Exit(false);
        }
    }
}
