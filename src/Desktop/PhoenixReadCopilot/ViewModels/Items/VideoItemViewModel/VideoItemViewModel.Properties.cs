using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Core.Models.Media;
using PhoenixReadCopilot.Toolkits;

namespace PhoenixReadCopilot.ViewModels.Items;

/// <summary>
/// 视频项视图模型.
/// </summary>
public sealed partial class VideoItemViewModel
{
    private readonly Action<VideoItemViewModel>? _removeAction;
    private readonly VideoFavoriteFolder? _favFolder;

    private static readonly ILogger<VideoItemViewModel> _logger = Ioc.Default.GetRequiredService<
        ILogger<VideoItemViewModel>
    >();

    /// <summary>
    /// 短剧数据实体 (如果是红果短剧).
    /// </summary>
    public Drama? Drama { get; init; }

    /// <summary>
    /// 阅读/观看历史条目 (如果是红果历史记录).
    /// </summary>
    public ReadHistoryItem? HistoryItem { get; init; }

    /// <summary>
    /// 演员数据实体 (如果是红果演员排行榜).
    /// </summary>
    public ActorLeaderboardItem? Actor { get; init; }

    /// <summary>
    /// 演员/用户标识 ID.
    /// </summary>
    public string? ActorId { get; init; }

    /// <summary>
    /// 排行榜名次.
    /// </summary>
    public int? Rank { get; init; }

    /// <summary>
    /// 是否有排名.
    /// </summary>
    public bool HasRank => Rank is > 0;

    /// <summary>
    /// 排名文案 (如 TOP 1).
    /// </summary>
    public string? RankText => HasRank ? $"TOP {Rank}" : null;

    /// <summary>
    /// 纯排名数字字符串.
    /// </summary>
    public string RankDisplay => Rank.HasValue ? Rank.Value.ToString() : string.Empty;

    /// <summary>
    /// 是否为前三名.
    /// </summary>
    public bool IsTopRank => Rank is >= 1 and <= 3;

    /// <summary>
    /// 演员副标题（作品数 · 追剧人数）.
    /// </summary>
    public string ActorSubtitles =>
        Actor?.SubTitles is { Count: > 0 } ? string.Join(" · ", Actor.SubTitles) : string.Empty;

    /// <summary>
    /// 演员代表作.
    /// </summary>
    public string ActorRepresentativeWork =>
        !string.IsNullOrEmpty(Actor?.RepresentativeWork)
            ? (
                Actor.RepresentativeWork.StartsWith("《")
                    ? Actor.RepresentativeWork
                    : $"《{Actor.RepresentativeWork}》"
            )
            : string.Empty;

    /// <summary>
    /// 演员热度/人气文案.
    /// </summary>
    public string ActorPopularity => Actor?.Popularity ?? string.Empty;

    /// <summary>
    /// 样式.
    /// </summary>
    public VideoCardStyle Style { get; }

    /// <summary>
    /// 标题.
    /// </summary>
    public string Title { get; init; }

    /// <summary>
    /// 副标题.
    /// </summary>
    public string Subtitle { get; init; }

    /// <summary>
    /// 封面.
    /// </summary>
    public Uri? Cover { get; init; }

    /// <summary>
    /// 作者 / 分类.
    /// </summary>
    public string Author { get; init; }

    /// <summary>
    /// 评分 (如 "9.5").
    /// </summary>
    public string? Score { get; init; }

    /// <summary>
    /// 是否有评分.
    /// </summary>
    public bool HasScore => !string.IsNullOrWhiteSpace(Score) && Score != "0" && Score != "0.0";

    /// <summary>
    /// 总集数.
    /// </summary>
    public int EpisodeCount { get; init; }

    /// <summary>
    /// 是否有集数或状态.
    /// </summary>
    public bool HasEpisodeCount => EpisodeCount > 0 || !string.IsNullOrWhiteSpace(Subtitle);

    /// <summary>
    /// 集数文本 (如 "全80集" / "更新至50集").
    /// </summary>
    public string? EpisodeCountText => $"{EpisodeCount}集";

    /// <summary>
    /// 标签列表.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>
    /// 分类.
    /// </summary>
    public string? Category { get; init; }

    /// <summary>
    /// 热度.
    /// </summary>
    public string? Heat { get; init; }

    /// <summary>
    /// 播放量原始文本.
    /// </summary>
    public string? Views { get; init; }

    /// <summary>
    /// 热度或播放量展示文本.
    /// </summary>
    public string? HeatOrPlayCountText
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Heat))
            {
                return Heat;
            }

            if (!string.IsNullOrWhiteSpace(Views))
            {
                return Views;
            }

            if (PlayCount is > 0)
            {
                return AppToolkit.FormatCount(Convert.ToInt64(PlayCount.Value));
            }

            return null;
        }
    }

    /// <summary>
    /// 发布时间.
    /// </summary>
    public string? PublishRelativeTime { get; init; }

    /// <summary>
    /// 作者头像.
    /// </summary>
    public Uri? Avatar { get; init; }

    /// <summary>
    /// 播放数.
    /// </summary>
    public double? PlayCount { get; init; }

    /// <summary>
    /// 弹幕数.
    /// </summary>
    public double? DanmakuCount { get; set; }

    /// <summary>
    /// 点赞数.
    /// </summary>
    public double? LikeCount { get; set; }

    /// <summary>
    /// 标签名称.
    /// </summary>
    public string? TagName { get; init; }

    /// <summary>
    /// 收集时间.
    /// </summary>
    public string? CollectTime { get; init; }

    /// <summary>
    /// 进度文本.
    /// </summary>
    public string? ProgressText { get; init; }

    /// <summary>
    /// 用户信息是否有效.
    /// </summary>
    public bool IsUserValid { get; init; }
}
