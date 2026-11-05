using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.Components;

namespace PhoenixReadCopilot.Controls.Message;

/// <summary>
/// 聊天会话消息主体.
/// </summary>
public sealed partial class ChatSessionBody : ChatMessageControlBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChatSessionBody"/> class.
    /// </summary>
    public ChatSessionBody() => InitializeComponent();

    /// <inheritdoc/>
    protected override void OnControlLoaded()
    {
        if (ViewModel is null)
        {
            return;
        }

        OnRequestScrollToBottom(default, default);
    }

    /// <inheritdoc/>
    protected override void OnControlUnloaded()
    {
        ViewModel?.RequestScrollToBottom -= OnRequestScrollToBottom;
    }

    /// <inheritdoc/>
    protected override void OnViewModelChanged(
        ChatMessageSectionDetailViewModel? oldValue,
        ChatMessageSectionDetailViewModel? newValue
    )
    {
        oldValue?.RequestScrollToBottom -= OnRequestScrollToBottom;

        if (newValue is null)
        {
            return;
        }

        newValue.RequestScrollToBottom += OnRequestScrollToBottom;
    }

    private void OnRequestScrollToBottom(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(
            DispatcherQueuePriority.Low,
            () =>
            {
                MessageView.ScrollTo(
                    0,
                    MessageView.ExtentHeight + MessageView.ViewportHeight + 100,
                    new ScrollingScrollOptions(ScrollingAnimationMode.Disabled)
                );
            }
        );
    }
}
