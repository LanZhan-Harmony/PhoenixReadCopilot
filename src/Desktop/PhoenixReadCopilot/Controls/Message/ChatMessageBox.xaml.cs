using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Windows.UI.Core;

namespace PhoenixReadCopilot.Controls.Message;

/// <summary>
/// 聊天消息框.
/// </summary>
public sealed partial class ChatMessageBox : ChatMessageControlBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChatMessageBox"/> class.
    /// </summary>
    public ChatMessageBox() => InitializeComponent();

    private void OnInputBoxKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            CoreVirtualKeyStates shiftState = InputKeyboardSource.GetKeyStateForCurrentThread(
                VirtualKey.Shift
            );
            bool isShiftDown =
                shiftState == CoreVirtualKeyStates.Down
                || shiftState == (CoreVirtualKeyStates.Down | CoreVirtualKeyStates.Locked);
            if (!isShiftDown)
            {
                e.Handled = true;
                ViewModel.SendMessageCommand.Execute(null);
            }
        }
    }

    private void OnItemClick(object sender, string e) => ViewModel.InputText += e;

    private void OnFlyoutClosed(object sender, object e) => InputBox.Focus(FocusState.Programmatic);
}
