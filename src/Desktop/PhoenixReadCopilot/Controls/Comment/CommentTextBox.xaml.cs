using System.Windows.Input;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Helpers;

namespace PhoenixReadCopilot.Controls.Comment;

/// <summary>
/// 评论输入框.
/// </summary>
internal sealed partial class CommentTextBox : UserControl
{
    /// <summary>
    /// 回复文本.
    /// </summary>
    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text),
        typeof(string),
        typeof(CommentTextBox),
        new PropertyMetadata(null)
    );

    /// <summary>
    /// 发送评论命令.
    /// </summary>
    public IAsyncRelayCommand SendCommand
    {
        get => (IAsyncRelayCommand)GetValue(SendCommandProperty);
        set => SetValue(SendCommandProperty, value);
    }

    public static readonly DependencyProperty SendCommandProperty = DependencyProperty.Register(
        nameof(SendCommand),
        typeof(IAsyncRelayCommand),
        typeof(CommentTextBox),
        new PropertyMetadata(null)
    );

    /// <summary>
    /// 评论提示.
    /// </summary>
    public string ReplyTip
    {
        get => (string)GetValue(ReplyTipProperty);
        set => SetValue(ReplyTipProperty, value);
    }

    public static readonly DependencyProperty ReplyTipProperty = DependencyProperty.Register(
        nameof(ReplyTip),
        typeof(string),
        typeof(CommentTextBox),
        new PropertyMetadata(null)
    );

    /// <summary>
    /// 重置已选择评论的命令.
    /// </summary>
    public ICommand ResetSelectedCommand
    {
        get => (ICommand)GetValue(ResetSelectedCommandProperty);
        set => SetValue(ResetSelectedCommandProperty, value);
    }

    public static readonly DependencyProperty ResetSelectedCommandProperty =
        DependencyProperty.Register(
            nameof(ResetSelectedCommand),
            typeof(ICommand),
            typeof(CommentTextBox),
            new PropertyMetadata(null)
        );

    public CommentTextBox()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateLoginState();
    }

    /// <summary>
    /// 更新登录状态.
    /// </summary>
    public void UpdateLoginState()
    {
        IAuthenticationService? authService = Ioc.Default.GetService<IAuthenticationService>();
        bool isAuthenticated = authService?.IsAuthenticated ?? false;
        ReplyBox.IsEnabled = isAuthenticated;
        ReplyBox.PlaceholderText = isAuthenticated
            ? Strings.Resources.ReplyPlaceholderText
            : "请先登录后发表评论";
    }

    private async void OnReplySubmittedAsync(
        AutoSuggestBox sender,
        AutoSuggestBoxQuerySubmittedEventArgs args
    )
    {
        IAuthenticationService? authService = Ioc.Default.GetService<IAuthenticationService>();
        bool isAuthenticated = authService?.IsAuthenticated ?? false;
        if (!isAuthenticated)
        {
            UpdateLoginState();
            return;
        }

        if (string.IsNullOrEmpty(Text))
        {
            return;
        }

        string formattedText = EmojiHelper.FormatEmojiText(Text);
        string finalText = string.IsNullOrEmpty(ReplyTip)
            ? formattedText
            : $"回复 @{ReplyTip} : {formattedText}";
        await SendCommand.ExecuteAsync(finalText);
        Text = string.Empty;
    }

    private void OnItemClick(object sender, string e) => Text += e;

    private void OnFlyoutClosed(object sender, object e) => ReplyBox.Focus(FocusState.Programmatic);
}
