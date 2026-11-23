using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;
using PhoenixReadCopilot.Contracts.Services;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Pages;
using PhoenixReadCopilot.ViewModels.View;
using WinUIEx;

namespace PhoenixReadCopilot.Forms;

/// <summary>
/// 初始窗口.
/// </summary>
internal sealed partial class StartupWindow : WindowEx
{
    private readonly IWindowService _windowService = Ioc.Default.GetRequiredService<IWindowService>();
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
        _windowService.RegisterWindow(this);

        RootFrame.Navigate(typeof(StartupPage), null, new SuppressNavigationTransitionInfo());

        Closed += OnClosed;
        _startupViewModel.RequestClose += OnRequestClose;
    }

    private void OnRequestClose(object? sender, EventArgs e)
    {
        Close();
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        _startupViewModel.RequestClose -= OnRequestClose;
        _windowService.UnregisterWindow(this);
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
