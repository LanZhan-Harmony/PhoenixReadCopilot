using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Core.Models.Pin;
using PhoenixReadCopilot.Pages.Overlay;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.ViewModels.Items;

/// <summary>
/// 演员排行榜项视图模型.
/// </summary>
public sealed partial class ActorItemViewModel : ViewModelBase<ActorLeaderboardItem>
{
    private readonly Action<ActorItemViewModel>? _removeAction;

    /// <summary>
    /// 演员/用户标识 ID.
    /// </summary>
    public string ActorId { get; }

    /// <summary>
    /// 演员姓名.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 演员头像 Uri.
    /// </summary>
    public Uri? Avatar { get; }

    /// <summary>
    /// 人气热度文案 (如 "700万人气").
    /// </summary>
    public string Popularity { get; }

    /// <summary>
    /// 代表作品名称.
    /// </summary>
    public string RepresentativeWork { get; }

    /// <summary>
    /// 代表作品格式化文案 (带书名号).
    /// </summary>
    public string RepresentativeWorkDisplay =>
        !string.IsNullOrWhiteSpace(RepresentativeWork)
            ? (
                RepresentativeWork.StartsWith('《')
                    ? RepresentativeWork
                    : $"《{RepresentativeWork}》"
            )
            : string.Empty;

    /// <summary>
    /// 是否有代表作品.
    /// </summary>
    public bool HasRepresentativeWork => !string.IsNullOrWhiteSpace(RepresentativeWork);

    /// <summary>
    /// 副标题标签列表 (如 ["10部作品", "双周追剧200万人"]).
    /// </summary>
    public IReadOnlyList<string> SubTitleList { get; }

    /// <summary>
    /// 副标题拼接文本.
    /// </summary>
    public string Subtitles =>
        SubTitleList is { Count: > 0 } ? string.Join(" · ", SubTitleList) : string.Empty;

    /// <summary>
    /// 是否有副标题.
    /// </summary>
    public bool HasSubtitles => SubTitleList is { Count: > 0 };

    /// <summary>
    /// 排名变化类型 (0: 维持不变, 1: 上升, 2: 下降).
    /// </summary>
    public int RankChangeType { get; }

    /// <summary>
    /// 排名变化数值.
    /// </summary>
    public int RankChangeValue { get; }

    /// <summary>
    /// 是否排名上升.
    /// </summary>
    public bool IsRankUp => RankChangeType == 1 && RankChangeValue > 0;

    /// <summary>
    /// 是否排名下降.
    /// </summary>
    public bool IsRankDown => RankChangeType == 2 && RankChangeValue > 0;

    /// <summary>
    /// 是否排名持平或无变化.
    /// </summary>
    public bool IsRankSame => !IsRankUp && !IsRankDown;

    /// <summary>
    /// 排名变化数值文本.
    /// </summary>
    public string RankChangeValueText =>
        RankChangeValue > 0 ? RankChangeValue.ToString() : string.Empty;

    /// <summary>
    /// 榜单排名.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRank))]
    [NotifyPropertyChangedFor(nameof(RankDisplay))]
    [NotifyPropertyChangedFor(nameof(IsTopRank))]
    public partial int? Rank { get; set; }

    /// <summary>
    /// 是否有排名.
    /// </summary>
    public bool HasRank => Rank is > 0;

    /// <summary>
    /// 纯排名数字显示.
    /// </summary>
    public string RankDisplay => Rank.HasValue ? Rank.Value.ToString() : string.Empty;

    /// <summary>
    /// 是否为前三名.
    /// </summary>
    public bool IsTopRank => Rank is >= 1 and <= 3;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActorItemViewModel"/> class.
    /// </summary>
    /// <param name="data">演员排行榜条目原始数据.</param>
    /// <param name="removeAction">移除回调（可选）.</param>
    public ActorItemViewModel(
        ActorLeaderboardItem data,
        Action<ActorItemViewModel>? removeAction = null
    )
        : base(data)
    {
        _removeAction = removeAction;
        ActorId = !string.IsNullOrEmpty(data.UserId) ? data.UserId : data.ActorId;
        Name = data.Name;
        Rank = data.Position > 0 ? data.Position : null;

        if (
            !string.IsNullOrEmpty(data.AvatarUrl)
            && Uri.TryCreate(data.AvatarUrl, UriKind.Absolute, out Uri? avatarUri)
        )
        {
            Avatar = avatarUri;
        }

        Popularity = data.Popularity;
        RepresentativeWork = data.RepresentativeWork;
        SubTitleList = data.SubTitles ?? [];
        RankChangeType = data.RankChangeType;
        RankChangeValue = data.RankChangeValue;
    }

    [RelayCommand]
    private void ShowUserSpace()
    {
        ItemServices.Navigation.NavigateToOver(typeof(UserSpacePage), Data);
    }

    [RelayCommand]
    private void Pin()
    {
        string coverUri = Avatar?.ToString() ?? string.Empty;
        PinItem pinItem = new(ActorId, Name, coverUri, PinType.User);
        ItemServices.Pinner.AddItem(pinItem);
    }
}
