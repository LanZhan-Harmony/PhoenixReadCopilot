using CommunityToolkit.Mvvm.Input;
using PhoenixReadApi;
using PhoenixReadCopilot.Pages.Overlay;

namespace PhoenixReadCopilot.ViewModels.Items;

/// <summary>
/// 用户空间短剧剧评条目模型.
/// </summary>
internal sealed partial class UserReviewItemViewModel : ViewModelBase<UserReview>
{
    /// <summary>
    /// 剧评 ID.
    /// </summary>
    public string Id => Data.Id;

    /// <summary>
    /// 短剧 ID.
    /// </summary>
    public string DramaId => Data.Drama.Id;

    /// <summary>
    /// 短剧标题.
    /// </summary>
    public string DramaTitle => Data.Drama.Title;

    /// <summary>
    /// 短剧封面 URL.
    /// </summary>
    public string DramaCoverUrl => Data.Drama.CoverUrl;

    /// <summary>
    /// 剧评内容.
    /// </summary>
    public string Content => Data.Content;

    /// <summary>
    /// 评分（10分制）.
    /// </summary>
    public int Rating => Data.Rating;

    /// <summary>
    /// 评分后缀文案（如“观看2小时后点评”）.
    /// </summary>
    public string RatingText => Data.RatingText;

    /// <summary>
    /// 获赞数量.
    /// </summary>
    public long LikeCount => Data.LikeCount;

    /// <summary>
    /// 回复总数.
    /// </summary>
    public long ReplyCount => Data.ReplyCount;

    /// <summary>
    /// 配图列表.
    /// </summary>
    public IReadOnlyList<CommentImage> Images => Data.Images;

    /// <summary>
    /// 是否有配图.
    /// </summary>
    public bool HasImages => Data.Images.Count > 0;

    /// <summary>
    /// 格式化时间.
    /// </summary>
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

    public UserReviewItemViewModel(UserReview review)
        : base(review) { }

    [RelayCommand]
    private void PlayDrama()
    {
        if (!string.IsNullOrEmpty(DramaId))
        {
            ItemServices.Navigation.NavigateToOver(typeof(VideoPlayerPage), DramaId);
        }
    }
}
