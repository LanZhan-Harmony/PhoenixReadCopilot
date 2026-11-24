using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.ViewModels.Core;

namespace PhoenixReadCopilot.Controls.Danmaku;

/// <summary>
/// 弹幕框.
/// </summary>
internal sealed partial class DanmakuBox : UserControl
{
    public DanmakuViewModel? ViewModel
    {
        get => (DanmakuViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(DanmakuViewModel),
        typeof(DanmakuBox),
        new PropertyMetadata(null)
    );

    public DanmakuBox()
    {
        InitializeComponent();
        Loaded += (_, _) => OnControlLoaded();
    }

    /// <summary>
    /// 输入框失去焦点.
    /// </summary>
    public event EventHandler InputLostFocus;

    /// <summary>
    /// 输入框获得焦点.
    /// </summary>
    public event EventHandler InputGotFocus;

    private void OnControlLoaded()
    {
        UpdateLoginState();
    }

    public void UpdateLoginState()
    {
        IAuthenticationService authService =
            Ioc.Default.GetRequiredService<IAuthenticationService>();
        bool isAuthenticated = authService.IsAuthenticated;

        DanmakuInputBox.IsEnabled = isAuthenticated;
        DanmakuInputBox.PlaceholderText = isAuthenticated
            ? Strings.Resources.DanmakuPlaceholder
            : "请先登录后发送弹幕";
    }

    private async void OnDanmakuInputBoxSubmittedAsync(
        AutoSuggestBox sender,
        AutoSuggestBoxQuerySubmittedEventArgs args
    )
    {
        if (string.IsNullOrEmpty(args.QueryText))
        {
            return;
        }

        IAuthenticationService authService =
            Ioc.Default.GetRequiredService<IAuthenticationService>();
        if (!authService.IsAuthenticated)
        {
            return;
        }

        sender.IsEnabled = false;
        await ViewModel.SendDanmakuAsync(args.QueryText);
        DispatcherQueue.TryEnqueue(() =>
        {
            UpdateLoginState();
            sender.Text = string.Empty;
        });
    }

    private void OnDisplayFlyoutClosed(object sender, object e) => ViewModel.ResetStyle();

    private void OnInputLostFocus(object sender, RoutedEventArgs e) =>
        InputLostFocus?.Invoke(this, EventArgs.Empty);

    private void OnInputFocus(object sender, RoutedEventArgs e) =>
        InputGotFocus?.Invoke(this, EventArgs.Empty);
}
