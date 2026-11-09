using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using PhoenixReadCopilot.ViewModels.Core;
using PhoenixReadCopilot.ViewModels.View;
using Richasy.BiliKernel.Bili;
using Richasy.BiliKernel.Bili.Authorization;
using Richasy.BiliKernel.Models.Authorization;
using WinUIEx;

namespace PhoenixReadCopilot.Forms;

/// <summary>
/// 网页登录窗口.
/// </summary>
public sealed partial class WebSignInWindow : WindowEx
{
    private readonly AppViewModel _appViewModel = Ioc.Default.GetRequiredService<AppViewModel>();
    private readonly StartupPageViewModel _startupViewModel =
        Ioc.Default.GetRequiredService<StartupPageViewModel>();
    private readonly IBiliTokenResolver _tokenResolver =
        Ioc.Default.GetRequiredService<IBiliTokenResolver>();
    private readonly IAuthenticationService _authService =
        Ioc.Default.GetRequiredService<IAuthenticationService>();
    private CancellationTokenSource _cancellationTokenSource = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="WebSignInWindow"/> class.
    /// </summary>
    public WebSignInWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        AppWindow.SetPresenter(AppWindowPresenterKind.CompactOverlay);

        Width = 1200;
        Height = 632;

        Title = Strings.Resources.AppName;

        this.CenterOnScreen();
        AppWindow.SetIcon("Assets/Logo/Icon.ico");
        SetTitleBar(TitleBar);
        _appViewModel.Windows.Add(this);

        Closed += OnClosed;
        Activated += OnActivated;
    }

    private void OnActivated(object sender, WindowActivatedEventArgs args) =>
        _startupViewModel.ExitCommand.Execute(false);

    private void OnClosed(object sender, WindowEventArgs args)
    {
        if (_cancellationTokenSource is not null)
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
        }

        Activated -= OnActivated;
        Closed -= OnClosed;
        BiliToken? localToken = _tokenResolver.GetToken();
        if (localToken != null)
        {
            _appViewModel.RestartCommand.Execute(null);
        }
    }

    private async void OnLoadedAsync(object sender, RoutedEventArgs e)
    {
        await MainView.EnsureCoreWebView2Async();
        MainView.CoreWebView2.Navigate("https://passport.bilibili.com/login");
    }

    private async void OnNavigationCompletedAsync(
        WebView2 sender,
        CoreWebView2NavigationCompletedEventArgs args
    )
    {
        if (sender.CoreWebView2.Source.StartsWith("https://passport.bilibili.com"))
        {
            await sender.CoreWebView2.ExecuteScriptAsync(
                "document.querySelector('.international-footer').remove();document.querySelector('.top-header').remove()"
            );
        }
        else if (sender.CoreWebView2.Source.StartsWith("https://www.bilibili.com"))
        {
            IReadOnlyList<CoreWebView2Cookie> cookies =
                await sender.CoreWebView2.CookieManager.GetCookiesAsync(sender.Source.AbsoluteUri);
            Dictionary<string, string> cookieDict = cookies
                .Select(p => (p.Name, p.Value))
                .ToDictionary();
            if (cookieDict.Count > 0)
            {
                await _authService.SignInAsync(
                    new AuthorizeExecutionSettings { Cookies = cookieDict },
                    _cancellationTokenSource.Token
                );
                _cancellationTokenSource = null;
                Close();
            }
        }
    }
}
