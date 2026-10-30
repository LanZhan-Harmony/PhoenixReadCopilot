using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using PhoenixReadCopilot.UI.ViewModels.Items;
using Richasy.BiliKernel.Bili.User;
using Richasy.BiliKernel.Models.User;

namespace PhoenixReadCopilot.UI.ViewModels.Components;

/// <summary>
/// 通知消息区块详情视图模型.
/// </summary>
public sealed partial class NotifyMessageSectionDetailViewModel
{
    private readonly IMessageService _service;
    private readonly ILogger<NotifyMessageSectionDetailViewModel> _logger;

    private long _offsetId;
    private long _offsetTime;
    private bool _preventLoadMore;

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial int UnreadCount { get; set; }

    /// <summary>
    /// 列表已完成更新.
    /// </summary>
    public event EventHandler ListUpdated;

    /// <summary>
    /// 条目列表.
    /// </summary>
    public ObservableCollection<NotifyMessageItemViewModel> Items { get; } = new();

    /// <summary>
    /// 通知消息类型.
    /// </summary>
    public NotifyMessageType Type { get; }
}
