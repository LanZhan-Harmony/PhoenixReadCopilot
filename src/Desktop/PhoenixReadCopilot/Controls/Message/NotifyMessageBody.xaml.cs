using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.Components;

namespace PhoenixReadCopilot.Controls.Message;

/// <summary>
/// 通知消息主体.
/// </summary>
public sealed partial class NotifyMessageBody : NotifyMessageControlBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotifyMessageBody"/> class.
    /// </summary>
    public NotifyMessageBody() => InitializeComponent();

    /// <inheritdoc/>
    protected override void OnControlLoaded()
    {
        MessageScrollView.ViewChanged += OnViewChanged;
        MessageScrollView.SizeChanged += OnScrollViewSizeChanged;
        if (ViewModel is null)
        {
            return;
        }

        CheckMessageCount();
    }

    /// <inheritdoc/>
    protected override void OnControlUnloaded()
    {
        ViewModel?.ListUpdated -= OnMessageListUpdatedAsync;

        MessageRepeater.ItemsSource = null;
        MessageScrollView.ViewChanged -= OnViewChanged;
        MessageScrollView.SizeChanged -= OnScrollViewSizeChanged;
    }

    /// <inheritdoc/>
    protected override void OnViewModelChanged(
        NotifyMessageSectionDetailViewModel? oldValue,
        NotifyMessageSectionDetailViewModel? newValue
    )
    {
        oldValue?.ListUpdated -= OnMessageListUpdatedAsync;

        if (newValue is null)
        {
            return;
        }

        newValue.ListUpdated += OnMessageListUpdatedAsync;
        MessageScrollView?.ChangeView(0, 0, default);
    }

    private async void OnMessageListUpdatedAsync(object? sender, EventArgs e)
    {
        await Task.Delay(500);
        CheckMessageCount();
    }

    private void OnViewChanged(object? sender, ScrollViewerViewChangedEventArgs args)
    {
        DispatcherQueue.TryEnqueue(
            DispatcherQueuePriority.Low,
            () =>
            {
                if (
                    MessageScrollView.ExtentHeight
                        - MessageScrollView.ViewportHeight
                        - MessageScrollView.VerticalOffset
                    <= 240
                )
                {
                    ViewModel.LoadItemsCommand.Execute(default);
                }
            }
        );
    }

    private void OnScrollViewSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (e.NewSize.Width > 100 && ViewModel is not null)
        {
            CheckMessageCount();
        }
    }

    private void CheckMessageCount()
    {
        DispatcherQueue.TryEnqueue(
            DispatcherQueuePriority.Low,
            () =>
            {
                if (MessageScrollView.ScrollableHeight <= 240 && ViewModel is not null)
                {
                    ViewModel.LoadItemsCommand.Execute(default);
                }
            }
        );
    }
}
