using CommunityToolkit.Mvvm.DependencyInjection;
using PhoenixReadCopilot.ViewModels.View;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.Pages;

/// <summary>
/// 消息页面.
/// </summary>
public sealed partial class MessagePage : MessagePageBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessagePage"/> class.
    /// </summary>
    public MessagePage() => InitializeComponent();

    /// <inheritdoc/>
    protected override void OnPageLoaded() => ViewModel.InitializeCommand.Execute(null);
}

/// <summary>
/// 消息页面基类.
/// </summary>
public abstract class MessagePageBase : LayoutPageBase<MessagePageViewModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessagePageBase"/> class.
    /// </summary>
    protected MessagePageBase() => ViewModel = Ioc.Default.GetRequiredService<MessagePageViewModel>();
}
