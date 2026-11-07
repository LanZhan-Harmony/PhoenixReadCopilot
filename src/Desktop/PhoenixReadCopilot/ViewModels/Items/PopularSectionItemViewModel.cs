using PhoenixReadApi;
using PhoenixReadCopilot.Core.Enums;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.ViewModels.Items;

/// <summary>
/// 流行与榜单区块项视图模型.
/// </summary>
public sealed class PopularSectionItemViewModel : ViewModelBase, IPopularSectionItemViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PopularSectionItemViewModel"/> class.
    /// </summary>
    public PopularSectionItemViewModel(
        FluentIcons.Common.Symbol symbol,
        string title,
        PopularSectionType type,
        ShortDramaLeaderboardType? leaderboardType = null,
        bool isActorLeaderboard = false
    )
    {
        Icon = symbol;
        Title = title;
        Type = type;
        LeaderboardType = leaderboardType;
        IsActorLeaderboard = isActorLeaderboard;
    }

    /// <summary>
    /// 图标.
    /// </summary>
    public FluentIcons.Common.Symbol Icon { get; init; }

    /// <summary>
    /// 标题.
    /// </summary>
    public string Title { get; init; }

    /// <summary>
    /// 类型.
    /// </summary>
    public PopularSectionType Type { get; init; }

    /// <summary>
    /// 红果短剧榜单类型 (如果是短剧榜单).
    /// </summary>
    public ShortDramaLeaderboardType? LeaderboardType { get; init; }

    /// <summary>
    /// 是否为演员榜单.
    /// </summary>
    public bool IsActorLeaderboard { get; init; }

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        obj is PopularSectionItemViewModel model && Type == model.Type;

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Type);
}
