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
        PinItem pinItem = new(ActorId, Name, coverUri, PinContentType.User);
        ItemServices.Pinner.AddItemCommand.Execute(pinItem);
    }
}
