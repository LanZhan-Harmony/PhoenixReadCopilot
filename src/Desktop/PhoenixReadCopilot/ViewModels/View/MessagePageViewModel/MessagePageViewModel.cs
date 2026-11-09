using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PhoenixReadCopilot.Pages;
using PhoenixReadCopilot.Services;
using PhoenixReadCopilot.Strings;
using PhoenixReadCopilot.ViewModels.Components;
using PhoenixReadCopilot.ViewModels.Core;
using PhoenixReadCopilot.ViewModels.Items;
using Richasy.BiliKernel.Bili.User;
using Richasy.BiliKernel.Models.User;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 消息页面视图模型.
/// </summary>
public sealed partial class MessagePageViewModel : LayoutPageViewModelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessagePageViewModel"/> class.
    /// </summary>
    public MessagePageViewModel(
        IMessageService service,
        ILogger<MessagePageViewModel> logger,
        AccountViewModel accountViewModel,
        AppViewModel appViewModel,
        ISettingsService settingsService,
        NavigationViewModel navigationViewModel,
        ILoggerFactory loggerFactory
    )
        : base(settingsService)
    {
        _service = service;
        _logger = logger;
        _accountViewModel = accountViewModel;
        _appViewModel = appViewModel;
        _navigationViewModel = navigationViewModel;
        _loggerFactory = loggerFactory;
    }

    /// <inheritdoc/>
    protected override string GetPageKey() => nameof(MessagePage);

    [RelayCommand]
    private async Task InitializeAsync()
    {
        if (Sections.Count > 0)
        {
            RestoreSelection();
            SectionInitialized?.Invoke(this, EventArgs.Empty);
            return;
        }

        IsLoading = true;
        Sections.Add(
            new NotifyMessageSectionDetailViewModel(
                NotifyMessageType.Reply,
                _service,
                _loggerFactory.CreateLogger<NotifyMessageSectionDetailViewModel>()
            )
        );
        Sections.Add(
            new NotifyMessageSectionDetailViewModel(
                NotifyMessageType.At,
                _service,
                _loggerFactory.CreateLogger<NotifyMessageSectionDetailViewModel>()
            )
        );
        Sections.Add(
            new NotifyMessageSectionDetailViewModel(
                NotifyMessageType.Like,
                _service,
                _loggerFactory.CreateLogger<NotifyMessageSectionDetailViewModel>()
            )
        );
        await LoadChatSessionsAsync();
        IsLoading = false;
        RestoreSelection();
        SectionInitialized?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        Sections.Clear();
        _chatSessionOffset = null;
        _preventLoadMore = false;
        await InitializeAsync();
    }

    [RelayCommand]
    private async Task SelectSectionAsync(IMessageSectionDetailViewModel vm)
    {
        if (vm is null || vm == CurrentSection)
        {
            return;
        }

        string sectionSettingValue = string.Empty;
        if (vm is NotifyMessageSectionDetailViewModel nfVM)
        {
            sectionSettingValue = $"nf_{nfVM.Type}";
        }
        else if (vm is ChatMessageSectionDetailViewModel chatVM)
        {
            sectionSettingValue = $"chat_{chatVM.Data.User.Id}";
        }

        _settings.LastSelectedMessageSection = sectionSettingValue;
        CurrentSection = vm;
        await CurrentSection.InitializeCommand.ExecuteAsync(null);
        _accountViewModel.UpdateUnreadCommand.Execute(null);
    }

    [RelayCommand]
    private async Task UpdateUnreadCountAsync()
    {
        try
        {
            UnreadInformation unread = await _service.GetUnreadInformationAsync();
            foreach (
                NotifyMessageSectionDetailViewModel? item in Sections
                    .OfType<NotifyMessageSectionDetailViewModel>()
                    .ToList()
            )
            {
                if (item.Type == NotifyMessageType.Like)
                {
                    item.UnreadCount = unread.LikeCount;
                }
                else if (item.Type == NotifyMessageType.Reply)
                {
                    item.UnreadCount = unread.ReplyCount;
                }
                else if (item.Type == NotifyMessageType.At)
                {
                    item.UnreadCount = unread.AtCount;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "无法获取未读消息数量.");
        }
    }

    [RelayCommand]
    private async Task LoadChatSessionsAsync()
    {
        if (IsChatSessionLoading || _preventLoadMore)
        {
            return;
        }

        try
        {
            IsChatSessionLoading = true;
            (IReadOnlyList<ChatSession>? sessions, long offset, bool hasMore) =
                await _service.GetChatSessionsAsync(_chatSessionOffset);
            _preventLoadMore = !hasMore;
            _chatSessionOffset = offset;
            if (sessions is not null)
            {
                foreach (ChatSession item in sessions)
                {
                    Sections.Add(
                        new ChatMessageSectionDetailViewModel(
                            item,
                            _service,
                            _navigationViewModel,
                            _appViewModel,
                            _loggerFactory.CreateLogger<ChatMessageSectionDetailViewModel>()
                        )
                    );
                }

                ChatSessionsUpdated?.Invoke(this, EventArgs.Empty);
            }
        }
        catch (Exception ex)
        {
            _preventLoadMore = true;
            _logger.LogError(ex, "加载聊天会话时失败.");
            _appViewModel.ShowTipCommand.Execute(
                (Resources.FailedToLoadChatSessions, InfoType.Error)
            );
        }
        finally
        {
            IsChatSessionLoading = false;
        }
    }

    private void RestoreSelection()
    {
        string lastSelectedSection = _settings.LastSelectedMessageSection;
        UpdateUnreadCountCommand.Execute(null);
        if (string.IsNullOrEmpty(lastSelectedSection))
        {
            SelectSectionCommand.Execute(Sections.First());
            return;
        }

        bool isSelected = false;
        if (lastSelectedSection.StartsWith("nf"))
        {
            bool isSuccess = Enum.TryParse<NotifyMessageType>(
                lastSelectedSection.Replace("nf_", string.Empty),
                out NotifyMessageType type
            );
            if (isSuccess)
            {
                NotifyMessageSectionDetailViewModel? section = Sections
                    .OfType<NotifyMessageSectionDetailViewModel>()
                    .FirstOrDefault(p => p.Type == type);
                if (section is not null)
                {
                    isSelected = true;
                    SelectSectionCommand.Execute(section);
                }
            }
        }
        else if (lastSelectedSection.StartsWith("chat"))
        {
            string id = lastSelectedSection.Replace("chat_", string.Empty);
            ChatMessageSectionDetailViewModel? section = Sections
                .OfType<ChatMessageSectionDetailViewModel>()
                .FirstOrDefault(p => p.Data.User.Id == id);
            if (section is not null)
            {
                isSelected = true;
                SelectSectionCommand.Execute(section);
            }
        }

        if (!isSelected && CurrentSection is null)
        {
            SelectSectionCommand.Execute(Sections.First());
        }
    }
}
