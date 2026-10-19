using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Media.Animation;
using PhoenixReadCopilot.UI.Models.Constants;
using PhoenixReadCopilot.UI.Pages;
using PhoenixReadCopilot.UI.Toolkits;
using PhoenixReadCopilot.UI.ViewModels.Core;
using PhoenixReadCopilot.UI.ViewModels.View;
using Richasy.BiliKernel.Bili.Authorization;
using Richasy.WinUIKernel.Share.Base;
using WinUIEx;

namespace PhoenixReadCopilot.UI.Forms;

/// <summary>
/// 初始窗口.
/// </summary>
public sealed partial class StartupWindow : WindowBase
{
    private bool _isFirstActivated = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="StartupWindow"/> class.
    /// </summary>
    public StartupWindow()
    {
        InitializeComponent();
        IsMaximizable = false;
        IsMinimizable = false;
        IsResizable = false;
        AppWindow.SetPresenter(AppWindowPresenterKind.CompactOverlay);

        Width = 720;
        Height = 460;

        Title = ResourceToolkit.GetLocalizedString(StringNames.AppName);

        this.CenterOnScreen();
        this.SetIcon("Assets/logo.ico");
        this.SetTitleBar(TitleBar);
        this.Get<AppViewModel>().Windows.Add(this);

        RootFrame.Navigate(typeof(StartupPage), default, new SuppressNavigationTransitionInfo());

        Activated += OnActivated;
        Closed += OnClosed;
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        var localToken = this.Get<IBiliTokenResolver>().GetToken();
        if (localToken is null)
        {
            this.Get<StartupPageViewModel>().ExitCommand.Execute(default);
        }
    }

    private void OnActivated(object sender, WindowActivatedEventArgs args)
    {
        if (_isFirstActivated)
        {
            _isFirstActivated = false;
            return;
        }

        this.Get<StartupPageViewModel>().ReloadQRCodeCommand.Execute(default);
    }
}
