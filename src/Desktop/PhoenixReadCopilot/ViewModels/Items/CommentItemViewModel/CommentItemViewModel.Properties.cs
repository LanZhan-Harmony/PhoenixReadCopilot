using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.Logging;
using PhoenixReadCopilot.Core.Models.Appearance;

namespace PhoenixReadCopilot.ViewModels.Items;

/// <summary>
/// 评论项视图模型.
/// </summary>
public sealed partial class CommentItemViewModel
{
    private static readonly ILogger<CommentItemViewModel> _logger = Ioc.Default.GetRequiredService<
        ILogger<CommentItemViewModel>
    >();

    private readonly Action<CommentItemViewModel>? _markReplyTargetAction;

    [ObservableProperty]
    public partial bool IsLiked { get; set; }

    [ObservableProperty]
    public partial int LikeCount { get; set; }

    /// <summary>
    /// 评论唯一标识.
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// 评论文本正文.
    /// </summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>
    /// 子评论/回复条数.
    /// </summary>
    public int ChildCount { get; init; }

    /// <summary>
    /// 发布的相对时间.
    /// </summary>
    public string RelativeTime { get; init; } = string.Empty;

    /// <summary>
    /// 发布的实际时间.
    /// </summary>
    public string ActualTime { get; init; } = string.Empty;

    /// <summary>
    /// 用户头像.
    /// </summary>
    public Uri? Avatar { get; init; }

    /// <summary>
    /// 用户名.
    /// </summary>
    public string UserName { get; init; } = string.Empty;

    /// <summary>
    /// 用户 ID.
    /// </summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// 所属剧集 ID.
    /// </summary>
    public string? DramaId { get; init; }

    /// <summary>
    /// 所属分集视频 ID.
    /// </summary>
    public string? VideoId { get; init; }

    /// <summary>
    /// 是否为整剧剧评（false 则为分集评论）.
    /// </summary>
    public bool IsDramaReview { get; init; }

    /// <summary>
    /// 是否为置顶评论.
    /// </summary>
    public bool IsTop { get; init; }

    /// <summary>
    /// 数据本身引用（保留向后兼容）.
    /// </summary>
    public CommentItemViewModel Data => this;

    /// <summary>
    /// 富文本内容（包含文字和附图）.
    /// </summary>
    public EmoteText Content { get; init; }
}
