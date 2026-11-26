using CommunityToolkit.Mvvm.Input;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Models.Media;
using PhoenixReadCopilot.Pages.Overlay;

namespace PhoenixReadCopilot.ViewModels.Items;

/// <summary>
/// 红果圈动态条目模型（图文或短视频）.
/// </summary>
internal sealed partial class CircleFeedItemViewModel(CircleItem item) : ViewModelBase
{
    /// <summary>
    /// 动态条目数据实体.
    /// </summary>
    public CircleItem Data { get; } = item;
    public string Id => Data.Id;

    public CircleItemType Type => Data.Type;

    public bool IsVideo => Data.Type == CircleItemType.Video;

    public string Title => Data.Title;

    public string Text => Data.Text;

    public string CoverUrl => Data.CoverUrl;

    public int CoverWidth => Data.CoverWidth;

    public int CoverHeight => Data.CoverHeight;

    public string AuthorName => Data.Author.Name;

    public string AuthorAvatarUrl => Data.Author.AvatarUrl;

    public long LikeCount => Data.LikeCount;

    public long CommentCount => Data.CommentCount;

    public long ViewCount => Data.ViewCount;

    public string DramaId => Data.DramaId;

    public string DramaTitle => Data.DramaTitle;

    public bool HasDrama => !string.IsNullOrEmpty(Data.DramaId);

    public string PublishTime
    {
        get
        {
            if (Data.CreatedAtUnixSeconds <= 0)
            {
                return string.Empty;
            }

            DateTimeOffset time = DateTimeOffset
                .FromUnixTimeSeconds(Data.CreatedAtUnixSeconds)
                .ToLocalTime();
            return time.ToString("yyyy-MM-dd HH:mm");
        }
    }

    [RelayCommand]
    private void Open()
    {
        if (IsVideo)
        {
            if (!string.IsNullOrEmpty(DramaId))
            {
                PlayableMedia playable = new(
                    DramaId,
                    !string.IsNullOrEmpty(DramaTitle) ? DramaTitle : Title,
                    CoverUrl
                );
                ItemServices.Navigation.NavigateToOver(typeof(VideoPlayerPage), playable);
            }
        }
        else
        {
            ItemServices.Navigation.NavigateToOver(typeof(ArticlePage), Data);
        }
    }
}
