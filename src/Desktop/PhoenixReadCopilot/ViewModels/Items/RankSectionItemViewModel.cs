using PhoenixReadApi;
using PhoenixReadCopilot.Contracts.ViewModels;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.ViewModels.View;
using Symbol = FluentIcons.Common.Symbol;

namespace PhoenixReadCopilot.ViewModels.Items;

/// <summary>
/// 榜单区块项视图模型.
/// </summary>
internal sealed class RankSectionItemViewModel(
    Symbol symbol,
    string title,
    RankSectionType type,
    ShortDramaLeaderboardType? leaderboardType = null,
    bool isActorLeaderboard = false,
    RankPageViewModel? pageViewModel = null
) : ViewModelBase, IRankSectionItemViewModel
{
    /// <summary>
    /// 图标.
    /// </summary>
    public Symbol Icon { get; init; } = symbol;

    /// <summary>
    /// 标题.
    /// </summary>
    public string Title { get; init; } = title;

    /// <summary>
    /// 类型.
    /// </summary>
    public RankSectionType Type { get; init; } = type;

    /// <summary>
    /// 红果短剧榜单类型 (如果是短剧榜单).
    /// </summary>
    public ShortDramaLeaderboardType? LeaderboardType { get; init; } = leaderboardType;

    /// <summary>
    /// 是否为演员榜单.
    /// </summary>
    public bool IsActorLeaderboard { get; init; } = isActorLeaderboard;

    /// <summary>
    /// 所属榜单页面视图模型.
    /// </summary>
    public RankPageViewModel? PageViewModel { get; set; } = pageViewModel;

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        obj is RankSectionItemViewModel model && Type == model.Type;

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Type);
}
