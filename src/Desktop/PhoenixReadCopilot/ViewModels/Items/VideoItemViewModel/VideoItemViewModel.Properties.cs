using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Toolkits;
using Richasy.BiliKernel.Models.Media;

namespace PhoenixReadCopilot.ViewModels.Items;

/// <summary>
/// 视频项视图模型.
/// </summary>
public sealed partial class VideoItemViewModel
{
    private readonly Action<VideoItemViewModel>? _removeAction;
    private readonly VideoFavoriteFolder? _favFolder;

    private static readonly ILogger<VideoItemViewModel> _logger = Ioc.Default.GetRequiredService<
        ILogger<VideoItemViewModel>
    >();

    /// <summary>
    /// 短剧数据实体 (如果是红果短剧).
    /// </summary>
    public Drama? Drama { get; init; }

    /// <summary>
    /// 样式.
    /// </summary>
    public VideoCardStyle Style { get; }

    /// <summary>
    /// 标题.
    /// </summary>
    public string Title { get; init; }

    /// <summary>
    /// 副标题.
    /// </summary>
    public string Subtitle { get; init; }

    /// <summary>
    /// 封面.
    /// </summary>
    public Uri? Cover { get; init; }

    /// <summary>
    /// 作者 / 分类.
    /// </summary>
    public string Author { get; init; }

    /// <summary>
    /// 评分 (如 "9.5").
    /// </summary>
    public string? Score { get; init; }

    /// <summary>
    /// 是否有评分.
    /// </summary>
    public bool HasScore => !string.IsNullOrWhiteSpace(Score) && Score != "0" && Score != "0.0";

    /// <summary>
    /// 总集数.
    /// </summary>
    public int EpisodeCount { get; init; }

    /// <summary>
    /// 是否有集数或状态.
    /// </summary>
    public bool HasEpisodeCount => EpisodeCount > 0 || !string.IsNullOrWhiteSpace(Subtitle);

    /// <summary>
    /// 集数文本 (如 "全80集" / "更新至50集").
    /// </summary>
    public string? EpisodeCountText => $"{EpisodeCount}集";

    /// <summary>
    /// 标签列表.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>
    /// 分类.
    /// </summary>
    public string? Category { get; init; }

    /// <summary>
    /// 热度.
    /// </summary>
    public string? Heat { get; init; }

    /// <summary>
    /// 播放量原始文本.
    /// </summary>
    public string? Views { get; init; }

    /// <summary>
    /// 热度或播放量展示文本.
    /// </summary>
    public string? HeatOrPlayCountText
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Heat))
            {
                return Heat;
            }

            if (!string.IsNullOrWhiteSpace(Views))
            {
                return Views;
            }

            if (PlayCount is > 0)
            {
                return AppToolkit.FormatCount(System.Convert.ToInt64(PlayCount.Value));
            }

            return null;
        }
    }

    /// <summary>
    /// 发布时间.
    /// </summary>
    public string? PublishRelativeTime { get; init; }

    /// <summary>
    /// 作者头像.
    /// </summary>
    public Uri? Avatar { get; init; }

    /// <summary>
    /// 播放数.
    /// </summary>
    public double? PlayCount { get; init; }

    /// <summary>
    /// 弹幕数.
    /// </summary>
    public double? DanmakuCount { get; set; }

    /// <summary>
    /// 点赞数.
    /// </summary>
    public double? LikeCount { get; set; }

    /// <summary>
    /// 标签名称.
    /// </summary>
    public string? TagName { get; init; }

    /// <summary>
    /// 收集时间.
    /// </summary>
    public string? CollectTime { get; init; }

    /// <summary>
    /// 进度文本.
    /// </summary>
    public string? ProgressText { get; init; }

    /// <summary>
    /// 用户信息是否有效.
    /// </summary>
    public bool IsUserValid { get; init; }
}
