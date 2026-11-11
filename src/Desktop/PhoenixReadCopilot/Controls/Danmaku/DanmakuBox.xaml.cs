using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadApi;

namespace PhoenixReadCopilot.Controls.Danmaku;

/// <summary>
/// 弹幕框.
/// </summary>
public sealed partial class DanmakuBox : DanmakuControlBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DanmakuBox"/> class.
    /// </summary>
    public DanmakuBox()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 输入框失去焦点.
    /// </summary>
    public event EventHandler InputLostFocus;

    /// <summary>
    /// 输入框获得焦点.
    /// </summary>
    public event EventHandler InputGotFocus;

    /// <inheritdoc/>
    protected override void OnControlLoaded()
    {
        base.OnControlLoaded();
        UpdateLoginState();
    }

    public void UpdateLoginState()
    {
        PhoenixReadApiClient? apiClient = Ioc.Default.GetService<PhoenixReadApiClient>();
        bool isAuthenticated = apiClient?.GetAuthenticationState().IsAuthenticated ?? false;

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

        PhoenixReadApiClient? apiClient = Ioc.Default.GetService<PhoenixReadApiClient>();
        bool isAuthenticated = apiClient?.GetAuthenticationState().IsAuthenticated ?? false;
        if (!isAuthenticated)
        {
            return;
        }

        sender.IsEnabled = false;
        await ViewModel.SendDanmakuCommand.ExecuteAsync(args.QueryText);
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
