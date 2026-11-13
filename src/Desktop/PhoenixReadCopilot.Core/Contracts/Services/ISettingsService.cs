using PhoenixReadCopilot.Core.Models.Settings;

namespace PhoenixReadCopilot.Core.Contracts.Services;

/// <summary>
/// 应用程序设置服务接口.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// 全局应用设置.
    /// </summary>
    AppSettings Settings { get; set; }

    /// <summary>
    /// 异步将设置保存到磁盘.
    /// </summary>
    Task SaveToDiskAsync();

    /// <summary>
    /// 立即将待保存的设置冲刷写入磁盘并等待完成.
    /// </summary>
    Task FlushAsync();
}
