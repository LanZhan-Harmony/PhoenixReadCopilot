using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;
using PhoenixReadCopilot.Pages;
using PhoenixReadCopilot.ViewModels.Core;
using PhoenixReadCopilot.ViewModels.View;
using Richasy.BiliKernel.Bili.Authorization;
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
    private readonly IBiliTokenResolver _tokenResolver =
        Ioc.Default.GetRequiredService<IBiliTokenResolver>();
    private bool _isFirstActivated = true;

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

        RootFrame.Navigate(typeof(StartupPage), default, new SuppressNavigationTransitionInfo());

        Activated += OnActivated;
        Closed += OnClosed;
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        var localToken = _tokenResolver.GetToken();
        if (localToken is null)
        {
            _startupViewModel.ExitCommand.Execute(default);
        }
    }

    private void OnActivated(object sender, WindowActivatedEventArgs args)
    {
        if (_isFirstActivated)
        {
            _isFirstActivated = false;
            return;
        }

        _startupViewModel.ReloadQRCodeCommand.Execute(default);
    }
}
