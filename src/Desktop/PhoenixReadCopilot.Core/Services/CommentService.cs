using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Contracts.Services;
using ZLogger;

namespace PhoenixReadCopilot.Core.Services;

/// <summary>
/// 评论与剧评服务实现，统一管理分集评论、整剧剧评与楼中楼回复.
/// </summary>
public sealed class CommentService(
    PhoenixReadApiClient apiClient,
    IAuthenticationService authService,
    ILogger<CommentService> logger
) : ICommentService
{
    private readonly PhoenixReadApiClient _apiClient = apiClient;
    private readonly IAuthenticationService _authService = authService;
    private readonly ILogger<CommentService> _logger = logger;

    /// <inheritdoc/>
    public async Task<CommentPage> GetEpisodeCommentsAsync(
        string dramaId,
        string videoId,
        int count = 20,
        string cursor = "",
        CommentSortOrder sort = CommentSortOrder.Hot,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dramaId);
        ArgumentException.ThrowIfNullOrWhiteSpace(videoId);

        try
        {
            return await _apiClient.GetEpisodeCommentsAsync(
                dramaId,
                videoId,
                count,
                cursor,
                sort,
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.ZLogError(
                ex,
                $"获取分集评论失败: dramaId={dramaId}, videoId={videoId}, cursor={cursor}"
            );
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<DramaReviewPage> GetDramaReviewsAsync(
        string dramaId,
        int count = 20,
        string cursor = "",
        CommentSortOrder sort = CommentSortOrder.Hot,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dramaId);

        try
        {
            return await _apiClient.GetDramaReviewsAsync(
                dramaId,
                count,
                cursor,
                sort,
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"获取整剧剧评失败: dramaId={dramaId}, cursor={cursor}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<CommentReplyPage> GetEpisodeCommentRepliesAsync(
        string commentId,
        string dramaId,
        string videoId,
        int count = 20,
        string cursor = "",
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(dramaId);

        try
        {
            return await _apiClient.GetEpisodeCommentRepliesAsync(
                commentId,
                dramaId,
                videoId,
                count,
                cursor,
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.ZLogError(
                ex,
                $"获取分集评论回复失败: commentId={commentId}, videoId={videoId}"
            );
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<CommentReplyPage> GetDramaReviewRepliesAsync(
        string commentId,
        string dramaId,
        int count = 20,
        string cursor = "",
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(dramaId);

        try
        {
            return await _apiClient.GetDramaReviewRepliesAsync(
                commentId,
                dramaId,
                count,
                cursor,
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"获取剧评回复失败: commentId={commentId}, dramaId={dramaId}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<PhoenixOperationResult> SendEpisodeCommentAsync(
        string dramaId,
        string videoId,
        string text,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dramaId);
        ArgumentException.ThrowIfNullOrWhiteSpace(videoId);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        if (!_authService.IsAuthenticated)
        {
            return new PhoenixOperationResult { Succeeded = false, Message = "请先登录" };
        }

        try
        {
            return await _apiClient.SendEpisodeCommentAsync(
                dramaId,
                videoId,
                text,
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"发送分集评论失败: dramaId={dramaId}, videoId={videoId}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<PhoenixOperationResult> SendDramaReviewAsync(
        string dramaId,
        double rating,
        string text,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dramaId);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        if (!_authService.IsAuthenticated)
        {
            return new PhoenixOperationResult { Succeeded = false, Message = "请先登录" };
        }

        try
        {
            return await _apiClient.SendDramaReviewAsync(dramaId, rating, text, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"发送剧评失败: dramaId={dramaId}, rating={rating}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<PhoenixOperationResult> SendEpisodeCommentReplyAsync(
        string commentId,
        string dramaId,
        string videoId,
        string text,
        string replyToUserId = "",
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(dramaId);
        ArgumentException.ThrowIfNullOrWhiteSpace(videoId);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        if (!_authService.IsAuthenticated)
        {
            return new PhoenixOperationResult { Succeeded = false, Message = "请先登录" };
        }

        try
        {
            return await _apiClient.SendEpisodeCommentReplyAsync(
                commentId,
                dramaId,
                videoId,
                text,
                replyToUserId,
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.ZLogError(
                ex,
                $"发送分集评论回复失败: commentId={commentId}, videoId={videoId}"
            );
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<PhoenixOperationResult> SendDramaReviewReplyAsync(
        string commentId,
        string dramaId,
        string text,
        string replyToUserId = "",
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(dramaId);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        if (!_authService.IsAuthenticated)
        {
            return new PhoenixOperationResult { Succeeded = false, Message = "请先登录" };
        }

        try
        {
            return await _apiClient.SendDramaReviewReplyAsync(
                commentId,
                dramaId,
                text,
                replyToUserId,
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"发送剧评回复失败: commentId={commentId}, dramaId={dramaId}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<PhoenixOperationResult> SetCommentLikeAsync(
        string commentId,
        string groupId,
        bool active = true,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(groupId);

        if (!_authService.IsAuthenticated)
        {
            return new PhoenixOperationResult { Succeeded = false, Message = "请先登录" };
        }

        try
        {
            return await _apiClient.SetCommentLikeAsync(
                commentId,
                groupId,
                active,
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"设置评论点赞状态失败: commentId={commentId}, active={active}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<PhoenixOperationResult> SetDramaReviewLikeAsync(
        string commentId,
        string dramaId,
        bool active = true,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(dramaId);

        if (!_authService.IsAuthenticated)
        {
            return new PhoenixOperationResult { Succeeded = false, Message = "请先登录" };
        }

        try
        {
            return await _apiClient.SetDramaReviewLikeAsync(
                commentId,
                dramaId,
                active,
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.ZLogError(
                ex,
                $"设置剧评点赞状态失败: commentId={commentId}, dramaId={dramaId}, active={active}"
            );
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<PhoenixOperationResult> SetReplyLikeAsync(
        string replyId,
        string groupId,
        bool active = true,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(replyId);
        ArgumentException.ThrowIfNullOrWhiteSpace(groupId);

        if (!_authService.IsAuthenticated)
        {
            return new PhoenixOperationResult { Succeeded = false, Message = "请先登录" };
        }

        try
        {
            return await _apiClient.SetReplyLikeAsync(replyId, groupId, active, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"设置回复点赞状态失败: replyId={replyId}, active={active}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<PhoenixOperationResult> DeleteCommentAsync(
        string commentId,
        string groupId,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(groupId);

        if (!_authService.IsAuthenticated)
        {
            return new PhoenixOperationResult { Succeeded = false, Message = "请先登录" };
        }

        try
        {
            return await _apiClient.DeleteCommentAsync(commentId, groupId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"删除评论失败: commentId={commentId}, groupId={groupId}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<PhoenixOperationResult> DeleteDramaReviewAsync(
        string commentId,
        string dramaId,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(dramaId);

        if (!_authService.IsAuthenticated)
        {
            return new PhoenixOperationResult { Succeeded = false, Message = "请先登录" };
        }

        try
        {
            return await _apiClient.DeleteDramaReviewAsync(commentId, dramaId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"删除剧评失败: commentId={commentId}, dramaId={dramaId}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<PhoenixOperationResult> DeleteReplyAsync(
        string replyId,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(replyId);

        if (!_authService.IsAuthenticated)
        {
            return new PhoenixOperationResult { Succeeded = false, Message = "请先登录" };
        }

        try
        {
            return await _apiClient.DeleteReplyAsync(replyId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"删除回复失败: replyId={replyId}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<CommentPage> GetCirclePostCommentsAsync(
        string postId,
        int count = 20,
        string cursor = "",
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(postId);

        try
        {
            return await _apiClient.GetCirclePostCommentsAsync(
                postId,
                count,
                cursor,
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"获取红果圈帖子评论失败: postId={postId}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<CommentReplyPage> GetCirclePostCommentRepliesAsync(
        string commentId,
        string postId,
        int count = 20,
        string cursor = "",
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(postId);

        try
        {
            return await _apiClient.GetCirclePostCommentRepliesAsync(
                commentId,
                postId,
                count,
                cursor,
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.ZLogError(
                ex,
                $"获取红果圈帖子回复失败: commentId={commentId}, postId={postId}"
            );
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<PhoenixOperationResult> LikeCirclePostCommentAsync(
        string commentId,
        string postId,
        bool active = true,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(postId);

        if (!_authService.IsAuthenticated)
        {
            return new PhoenixOperationResult { Succeeded = false, Message = "请先登录" };
        }

        try
        {
            return await _apiClient.SetCirclePostCommentLikeAsync(
                commentId,
                postId,
                active,
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.ZLogError(
                ex,
                $"点赞红果圈帖子评论失败: commentId={commentId}, postId={postId}"
            );
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<PhoenixOperationResult> LikeCirclePostReplyAsync(
        string replyId,
        string postId,
        bool active = true,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(replyId);
        ArgumentException.ThrowIfNullOrWhiteSpace(postId);

        if (!_authService.IsAuthenticated)
        {
            return new PhoenixOperationResult { Succeeded = false, Message = "请先登录" };
        }

        try
        {
            return await _apiClient.SetCirclePostReplyLikeAsync(
                replyId,
                postId,
                active,
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"点赞红果圈帖子回复失败: replyId={replyId}, postId={postId}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<PhoenixOperationResult> DeleteCirclePostCommentAsync(
        string commentId,
        string postId,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(postId);

        if (!_authService.IsAuthenticated)
        {
            return new PhoenixOperationResult { Succeeded = false, Message = "请先登录" };
        }

        try
        {
            return await _apiClient.DeleteCirclePostCommentAsync(
                commentId,
                postId,
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.ZLogError(
                ex,
                $"删除红果圈帖子评论失败: commentId={commentId}, postId={postId}"
            );
            throw;
        }
    }
}
