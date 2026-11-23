using PhoenixReadCopilot.Core.Models.Subtitle;

namespace PhoenixReadCopilot.Core.Contracts.Services;

/// <summary>
/// 视频字幕服务接口.
/// </summary>
public interface ISubtitleService
{
    /// <summary>
    /// 获取字幕元数据列表.
    /// </summary>
    Task<IReadOnlyList<SubtitleMeta>> GetSubtitleMetasAsync(string aid, string cid);

    /// <summary>
    /// 获取字幕详情.
    /// </summary>
    Task<IReadOnlyList<SubtitleInformation>> GetSubtitleDetailAsync(SubtitleMeta meta);
}
