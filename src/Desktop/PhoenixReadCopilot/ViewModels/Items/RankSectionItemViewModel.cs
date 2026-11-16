using PhoenixReadApi;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.ViewModels.View;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.ViewModels.Items;

/// <summary>
/// 榜单区块项视图模型.
/// </summary>
public sealed class RankSectionItemViewModel : ViewModelBase, IRankSectionItemViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RankSectionItemViewModel"/> class.
    /// </summary>
    public RankSectionItemViewModel(
        FluentIcons.Common.Symbol symbol,
        string title,
        RankSectionType type,
        ShortDramaLeaderboardType? leaderboardType = null,
        bool isActorLeaderboard = false,
        RankPageViewModel? pageViewModel = null
    )
    {
        Icon = symbol;
        Title = title;
        Type = type;
        LeaderboardType = leaderboardType;
        IsActorLeaderboard = isActorLeaderboard;
        PageViewModel = pageViewModel;
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
    public RankSectionType Type { get; init; }

    /// <summary>
    /// 红果短剧榜单类型 (如果是短剧榜单).
    /// </summary>
    public ShortDramaLeaderboardType? LeaderboardType { get; init; }

    /// <summary>
    /// 是否为演员榜单.
    /// </summary>
    public bool IsActorLeaderboard { get; init; }

    /// <summary>
    /// 所属榜单页面视图模型.
    /// </summary>
    public RankPageViewModel? PageViewModel { get; set; }

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        obj is RankSectionItemViewModel model && Type == model.Type;

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Type);
}
