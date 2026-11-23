using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Models.Subtitle;

namespace PhoenixReadCopilot.Core.Services;

/// <summary>
/// 默认字幕服务实现（红果短剧暂未提供独立外挂字幕接口）.
/// </summary>
public sealed class SubtitleService : ISubtitleService
{
    /// <inheritdoc/>
    public Task<IReadOnlyList<SubtitleMeta>> GetSubtitleMetasAsync(string aid, string cid)
    {
        // TODO: 红果短剧暂未提供外挂字幕 API
        return Task.FromResult<IReadOnlyList<SubtitleMeta>>([]);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<SubtitleInformation>> GetSubtitleDetailAsync(SubtitleMeta meta)
    {
        return Task.FromResult<IReadOnlyList<SubtitleInformation>>([]);
    }
}
