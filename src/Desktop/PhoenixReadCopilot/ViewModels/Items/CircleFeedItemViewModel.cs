using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhoenixReadApi;
using PhoenixReadCopilot.Pages.Overlay;

namespace PhoenixReadCopilot.ViewModels.Items;

/// <summary>
/// 红果圈动态条目模型（图文或短视频）.
/// </summary>
public sealed partial class CircleFeedItemViewModel : ObservableObject
{
    private readonly CircleItem _item;

    public CircleItem Data => _item;

    public string Id => _item.Id;

    public CircleItemType Type => _item.Type;

    public bool IsVideo => _item.Type == CircleItemType.Video;

    public string Title => _item.Title;

    public string Text => _item.Text;

    public string CoverUrl => _item.CoverUrl;

    public int CoverWidth => _item.CoverWidth;

    public int CoverHeight => _item.CoverHeight;

    public string AuthorName => _item.Author.Name;

    public string AuthorAvatarUrl => _item.Author.AvatarUrl;

    public long LikeCount => _item.LikeCount;

    public long CommentCount => _item.CommentCount;

    public long ViewCount => _item.ViewCount;

    public string DramaId => _item.DramaId;

    public string DramaTitle => _item.DramaTitle;

    public bool HasDrama => !string.IsNullOrEmpty(_item.DramaId);

    public string PublishTime
    {
        get
        {
            if (_item.CreatedAtUnixSeconds <= 0)
            {
                return string.Empty;
            }

            DateTimeOffset time = DateTimeOffset
                .FromUnixTimeSeconds(_item.CreatedAtUnixSeconds)
                .ToLocalTime();
            return time.ToString("yyyy-MM-dd HH:mm");
        }
    }

    public CircleFeedItemViewModel(CircleItem item)
    {
        _item = item;
    }

    [RelayCommand]
    private void Open()
    {
        if (IsVideo)
        {
            if (!string.IsNullOrEmpty(DramaId))
            {
                ItemServices.Navigation.NavigateToOver(typeof(VideoPlayerPage), DramaId);
            }
        }
        else
        {
            ItemServices.Navigation.NavigateToOver(typeof(ArticlePage), _item);
        }
    }
}
