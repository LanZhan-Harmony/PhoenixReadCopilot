using PhoenixReadApi;

namespace PhoenixReadCopilot.Core.Contracts.Services;

/// <summary>
/// 评论与剧评服务接口，提供分集评论、整剧剧评、楼中楼回复及点赞/删除操作.
/// </summary>
public interface ICommentService
{
    /// <summary>
    /// 获取指定分集的一页评论.
    /// </summary>
    /// <param name="dramaId">剧集 ID.</param>
    /// <param name="videoId">分集视频 ID.</param>
    /// <param name="count">返回数量（默认 20）.</param>
    /// <param name="cursor">上一页游标，首次请求传空.</param>
    /// <param name="sort">排序规则（默认最热）.</param>
    /// <param name="cancellationToken">取消令牌.</param>
    /// <returns>评论分页数据.</returns>
    Task<CommentPage> GetEpisodeCommentsAsync(
        string dramaId,
        string videoId,
        int count = 20,
        string cursor = "",
        CommentSortOrder sort = CommentSortOrder.Hot,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 获取指定剧集的一页剧评.
    /// </summary>
    /// <param name="dramaId">剧集 ID.</param>
    /// <param name="count">返回数量（默认 20）.</param>
    /// <param name="cursor">上一页游标，首次请求传空.</param>
    /// <param name="sort">排序规则（默认最热）.</param>
    /// <param name="cancellationToken">取消令牌.</param>
    /// <returns>剧评分页数据.</returns>
    Task<CommentPage> GetDramaReviewsAsync(
        string dramaId,
        int count = 20,
        string cursor = "",
        CommentSortOrder sort = CommentSortOrder.Hot,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 获取分集评论的楼中楼回复列表.
    /// </summary>
    /// <param name="commentId">评论 ID.</param>
    /// <param name="dramaId">剧集 ID.</param>
    /// <param name="videoId">分集视频 ID.</param>
    /// <param name="count">返回数量（默认 20）.</param>
    /// <param name="cursor">上一页游标，首次请求传空.</param>
    /// <param name="cancellationToken">取消令牌.</param>
    /// <returns>回复分页数据.</returns>
    Task<CommentReplyPage> GetEpisodeCommentRepliesAsync(
        string commentId,
        string dramaId,
        string videoId,
        int count = 20,
        string cursor = "",
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 获取剧评的楼中楼回复列表.
    /// </summary>
    /// <param name="commentId">剧评 ID.</param>
    /// <param name="dramaId">剧集 ID.</param>
    /// <param name="count">返回数量（默认 20）.</param>
    /// <param name="cursor">上一页游标，首次请求传空.</param>
    /// <param name="cancellationToken">取消令牌.</param>
    /// <returns>回复分页数据.</returns>
    Task<CommentReplyPage> GetDramaReviewRepliesAsync(
        string commentId,
        string dramaId,
        int count = 20,
        string cursor = "",
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 发送分集评论.
    /// </summary>
    /// <param name="dramaId">剧集 ID.</param>
    /// <param name="videoId">分集视频 ID.</param>
    /// <param name="text">评论正文.</param>
    /// <param name="cancellationToken">取消令牌.</param>
    /// <returns>操作结果.</returns>
    Task<PhoenixOperationResult> SendEpisodeCommentAsync(
        string dramaId,
        string videoId,
        string text,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 发送剧评（评分与评语）.
    /// </summary>
    /// <param name="dramaId">剧集 ID.</param>
    /// <param name="rating">评分（0.5 ~ 5.0）.</param>
    /// <param name="text">剧评正文.</param>
    /// <param name="cancellationToken">取消令牌.</param>
    /// <returns>操作结果.</returns>
    Task<PhoenixOperationResult> SendDramaReviewAsync(
        string dramaId,
        double rating,
        string text,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 回复一条分集评论.
    /// </summary>
    /// <param name="commentId">评论 ID.</param>
    /// <param name="dramaId">剧集 ID.</param>
    /// <param name="videoId">分集视频 ID.</param>
    /// <param name="text">回复正文.</param>
    /// <param name="replyToUserId">被回复的用户 ID（可选）.</param>
    /// <param name="cancellationToken">取消令牌.</param>
    /// <returns>操作结果.</returns>
    Task<PhoenixOperationResult> SendEpisodeCommentReplyAsync(
        string commentId,
        string dramaId,
        string videoId,
        string text,
        string replyToUserId = "",
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 回复一条剧评.
    /// </summary>
    /// <param name="commentId">剧评 ID.</param>
    /// <param name="dramaId">剧集 ID.</param>
    /// <param name="text">回复正文.</param>
    /// <param name="replyToUserId">被回复的用户 ID（可选）.</param>
    /// <param name="cancellationToken">取消令牌.</param>
    /// <returns>操作结果.</returns>
    Task<PhoenixOperationResult> SendDramaReviewReplyAsync(
        string commentId,
        string dramaId,
        string text,
        string replyToUserId = "",
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 点赞或取消点赞分集评论.
    /// </summary>
    /// <param name="commentId">评论 ID.</param>
    /// <param name="groupId">所属视频 ID 或剧集 ID.</param>
    /// <param name="active">true 点赞，false 取消点赞.</param>
    /// <param name="cancellationToken">取消令牌.</param>
    /// <returns>操作结果.</returns>
    Task<PhoenixOperationResult> SetCommentLikeAsync(
        string commentId,
        string groupId,
        bool active = true,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 点赞或取消点赞整剧剧评.
    /// </summary>
    /// <param name="commentId">剧评 ID.</param>
    /// <param name="dramaId">所属剧集 ID.</param>
    /// <param name="active">true 点赞，false 取消点赞.</param>
    /// <param name="cancellationToken">取消令牌.</param>
    /// <returns>操作结果.</returns>
    Task<PhoenixOperationResult> SetDramaReviewLikeAsync(
        string commentId,
        string dramaId,
        bool active = true,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 点赞或取消点赞评论回复.
    /// </summary>
    /// <param name="replyId">回复 ID.</param>
    /// <param name="groupId">所属视频 ID 或剧集 ID.</param>
    /// <param name="active">true 点赞，false 取消点赞.</param>
    /// <param name="cancellationToken">取消令牌.</param>
    /// <returns>操作结果.</returns>
    Task<PhoenixOperationResult> SetReplyLikeAsync(
        string replyId,
        string groupId,
        bool active = true,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 删除分集评论.
    /// </summary>
    /// <param name="commentId">评论 ID.</param>
    /// <param name="groupId">所属视频 ID 或剧集 ID.</param>
    /// <param name="cancellationToken">取消令牌.</param>
    /// <returns>操作结果.</returns>
    Task<PhoenixOperationResult> DeleteCommentAsync(
        string commentId,
        string groupId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 删除剧评.
    /// </summary>
    /// <param name="commentId">剧评 ID.</param>
    /// <param name="dramaId">所属剧集 ID.</param>
    /// <param name="cancellationToken">取消令牌.</param>
    /// <returns>操作结果.</returns>
    Task<PhoenixOperationResult> DeleteDramaReviewAsync(
        string commentId,
        string dramaId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 删除回复.
    /// </summary>
    /// <param name="replyId">回复 ID.</param>
    /// <param name="cancellationToken">取消令牌.</param>
    /// <returns>操作结果.</returns>
    Task<PhoenixOperationResult> DeleteReplyAsync(
        string replyId,
        CancellationToken cancellationToken = default
    );
}
