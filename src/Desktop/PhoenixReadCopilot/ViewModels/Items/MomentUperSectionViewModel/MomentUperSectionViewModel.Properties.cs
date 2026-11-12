using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.Logging;
using PhoenixReadCopilot.Core.Models.Moment;

namespace PhoenixReadCopilot.ViewModels.Items;

/// <summary>
/// 动态点赞者条目视图模型.
/// </summary>
public sealed partial class MomentUperSectionViewModel
{
    private static ILogger<MomentUperSectionViewModel>? _logger;
    private readonly Action<MomentInformation>? _showCommentAction;
    private bool _preventLoadMore;
    private string? _offset;
    private string? _baseline;

    private static ILogger<MomentUperSectionViewModel> Logger =>
        _logger ??= Ioc.Default.GetRequiredService<ILogger<MomentUperSectionViewModel>>();

    [ObservableProperty]
    public partial bool HasUnread { get; set; }

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    /// <summary>
    /// 列表已完成更新.
    /// </summary>
    public event EventHandler? ListUpdated;

    /// <summary>
    /// 是否为全部动态区块.
    /// </summary>
    public bool IsTotal { get; init; }

    /// <summary>
    /// 头像.
    /// </summary>
    public Uri? Avatar { get; init; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// 条目列表.
    /// </summary>
    public ObservableCollection<MomentItemViewModel> Items { get; } = [];
}
