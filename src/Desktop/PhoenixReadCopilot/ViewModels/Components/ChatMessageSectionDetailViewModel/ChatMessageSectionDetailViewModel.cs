using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using Humanizer;
using Microsoft.Extensions.Logging;
using PhoenixReadCopilot.Pages.Overlay;
using PhoenixReadCopilot.ViewModels.Core;
using PhoenixReadCopilot.ViewModels.Items;
using Richasy.BiliKernel.Bili.User;
using Richasy.BiliKernel.Models.User;
using WinUIKernel.Share.Base;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.ViewModels.Components;

/// <summary>
/// 聊天消息区块详情视图模型.
/// </summary>
public sealed partial class ChatMessageSectionDetailViewModel
    : ViewModelBase<ChatSession>,
        IMessageSectionDetailViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChatMessageSectionDetailViewModel"/> class.
    /// </summary>
    public ChatMessageSectionDetailViewModel(
        ChatSession session,
        IMessageService service,
        NavigationViewModel navigationViewModel,
        AppViewModel appViewModel,
        ILogger<ChatMessageSectionDetailViewModel> logger
    )
        : base(session)
    {
        _service = service;
        _navigationViewModel = navigationViewModel;
        _appViewModel = appViewModel;
        _logger = logger;

        Avatar = session.User?.Avatar?.Uri;
        UserName = session.User?.Name;
        UnreadCount = session.UnreadCount;
        LastMessage = Data.LastMessage;
        LastMessageTime = Data.Time.Humanize(default, new CultureInfo("zh-CN"));
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        if (Messages?.Count > 0 || _preventLoadMore || IsEmpty)
        {
            return;
        }

        Messages = new();
        await LoadMessagesAsync();
    }

    [RelayCommand]
    private void ShowUserSpace() =>
        _navigationViewModel.NavigateToOver(typeof(UserSpacePage), Data.User);

    [RelayCommand]
    private async Task RefreshAsync()
    {
        Messages = new();
        _preventLoadMore = false;
        IsEmpty = false;
        await LoadMessagesAsync();
    }

    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrEmpty(InputText) || IsSending)
        {
            return;
        }

        try
        {
            IsSending = true;
            var now = DateTimeOffset.Now;
            var msg = await _service.SendChatMessageAsync(InputText, Data.User);
            if (msg is not null)
            {
                Messages.Add(new ChatMessageItemViewModel(msg));
                InputText = string.Empty;
            }

            LastMessage = msg.Content.Text;
            LastMessageTime = now.Humanize(default, new CultureInfo("zh-CN"));
            RequestScrollToBottom?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送消息时失败");
            _appViewModel.ShowTipCommand.Execute((Resources.FailedToSendMessage, InfoType.Error));
        }
        finally
        {
            IsSending = false;
            IsEmpty = Messages.Count == 0;
        }
    }

    private async Task LoadMessagesAsync()
    {
        if (IsLoading || _preventLoadMore)
        {
            return;
        }

        try
        {
            IsLoading = true;
            var (messages, maxCount, hasMore) = await _service.GetChatMessagesAsync(Data.User);
            if (messages is not null)
            {
                foreach (var item in messages)
                {
                    Messages.Add(new ChatMessageItemViewModel(item));
                }

                if (LastMessage != messages.Last().Content.Text)
                {
                    LastMessage = messages.Last().Content.Text;
                    LastMessageTime = messages
                        .Last()
                        .Time.Humanize(default, new CultureInfo("zh-CN"));
                }

                RequestScrollToBottom?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                _preventLoadMore = true;
                IsEmpty = true;
            }
        }
        catch (Exception ex)
        {
            _preventLoadMore = true;
            _logger.LogError(ex, "加载聊天消息时失败");
            _appViewModel.ShowTipCommand.Execute(
                (Resources.FailedToLoadChatMessages, InfoType.Error)
            );
        }
        finally
        {
            IsLoading = false;
            IsEmpty = Messages.Count == 0;
        }
    }
}
