using PhoenixReadApi;
using PhoenixReadCopilot.Core.Enums;

namespace PhoenixReadCopilot.ViewModels.Items;

/// <summary>
/// 排行榜短剧条目视图模型.
/// </summary>
internal sealed partial class RankDramaItemViewModel(
    Drama drama,
    int rank,
    VideoCardStyle style = VideoCardStyle.Rank
) : DramaItemViewModel(drama, style)
{
    /// <summary>
    /// 排行榜名次.
    /// </summary>
    public int Rank { get; } = rank;

    /// <summary>
    /// 是否有排名.
    /// </summary>
    public bool HasRank => Rank > 0;

    /// <summary>
    /// 排名文案 (如 TOP 1).
    /// </summary>
    public string? RankText => HasRank ? $"TOP {Rank}" : null;

    /// <summary>
    /// 纯排名数字文本.
    /// </summary>
    public string RankDisplay => Rank > 0 ? Rank.ToString() : string.Empty;

    /// <summary>
    /// 是否为前三名.
    /// </summary>
    public bool IsTopRank => Rank is >= 1 and <= 3;
}
