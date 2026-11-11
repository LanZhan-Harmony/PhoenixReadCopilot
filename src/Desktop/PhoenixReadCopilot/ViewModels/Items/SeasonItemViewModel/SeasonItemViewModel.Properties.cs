using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Enums;

namespace PhoenixReadCopilot.ViewModels.Items;

/// <summary>
/// 剧集条目视图模型.
/// </summary>
public sealed partial class SeasonItemViewModel
{
    private static ILogger<SeasonItemViewModel>? _logger;
    private readonly Action<SeasonItemViewModel>? _removeAction;

    private static ILogger<SeasonItemViewModel> Logger =>
        _logger ??= Ioc.Default.GetRequiredService<ILogger<SeasonItemViewModel>>();

    /// <summary>
    /// 标题.
    /// </summary>
    public string Title { get; init; }

    /// <summary>
    /// 副标题.
    /// </summary>
    public string? Subtitle { get; init; }

    /// <summary>
    /// 高亮内容.
    /// </summary>
    public string? Highlight { get; init; }

    /// <summary>
    /// 封面.
    /// </summary>
    public Uri Cover { get; init; }

    /// <summary>
    /// 评分.
    /// </summary>
    public double? Score { get; init; }

    /// <summary>
    /// 样式.
    /// </summary>
    public SeasonCardStyle Style { get; init; }

    /// <summary>
    /// 短剧数据实体.
    /// </summary>
    public Drama? Drama { get; init; }
}
