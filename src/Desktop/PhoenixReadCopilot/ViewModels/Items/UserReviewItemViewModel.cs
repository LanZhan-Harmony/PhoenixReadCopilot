using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhoenixReadApi;
using PhoenixReadCopilot.Pages.Overlay;

namespace PhoenixReadCopilot.ViewModels.Items;

/// <summary>
/// 用户空间短剧剧评条目模型.
/// </summary>
public sealed partial class UserReviewItemViewModel : ObservableObject
{
    private readonly UserReview _review;

    /// <summary>
    /// 原始数据实体.
    /// </summary>
    public UserReview Data => _review;

    /// <summary>
    /// 剧评 ID.
    /// </summary>
    public string Id => _review.Id;

    /// <summary>
    /// 短剧 ID.
    /// </summary>
    public string DramaId => _review.Drama.Id;

    /// <summary>
    /// 短剧标题.
    /// </summary>
    public string DramaTitle => _review.Drama.Title;

    /// <summary>
    /// 短剧封面 URL.
    /// </summary>
    public string DramaCoverUrl => _review.Drama.CoverUrl;

    /// <summary>
    /// 剧评内容.
    /// </summary>
    public string Content => _review.Content;

    /// <summary>
    /// 评分（10分制）.
    /// </summary>
    public int Rating => _review.Rating;

    /// <summary>
    /// 评分后缀文案（如“观看2小时后点评”）.
    /// </summary>
    public string RatingText => _review.RatingText;

    /// <summary>
    /// 获赞数量.
    /// </summary>
    public long LikeCount => _review.LikeCount;

    /// <summary>
    /// 回复总数.
    /// </summary>
    public long ReplyCount => _review.ReplyCount;

    /// <summary>
    /// 配图列表.
    /// </summary>
    public IReadOnlyList<CommentImage> Images => _review.Images;

    /// <summary>
    /// 是否有配图.
    /// </summary>
    public bool HasImages => _review.Images.Count > 0;

    /// <summary>
    /// 格式化时间.
    /// </summary>
    public string PublishTime
    {
        get
        {
            if (_review.CreatedAtUnixSeconds <= 0)
            {
                return string.Empty;
            }

            DateTimeOffset time = DateTimeOffset
                .FromUnixTimeSeconds(_review.CreatedAtUnixSeconds)
                .ToLocalTime();
            return time.ToString("yyyy-MM-dd HH:mm");
        }
    }

    public UserReviewItemViewModel(UserReview review)
    {
        _review = review;
    }

    [RelayCommand]
    private void PlayDrama()
    {
        if (!string.IsNullOrEmpty(DramaId))
        {
            ItemServices.Navigation.NavigateToOver(typeof(VideoPlayerPage), DramaId);
        }
    }
}
