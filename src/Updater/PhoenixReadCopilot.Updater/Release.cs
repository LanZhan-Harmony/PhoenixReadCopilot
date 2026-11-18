namespace PhoenixReadCopilot.Updater;

/// <summary>
/// 一个可用的发行版，包含版本号和对应架构的下载资源.
/// </summary>
/// <param name="Version">版本号 (如 0.1.0.0).</param>
/// <param name="Asset">对应当前架构的下载资源.</param>
/// <param name="IsPrerelease">是否为预发布版本.</param>
public sealed record AppRelease(Version Version, SourceAsset Asset, bool IsPrerelease = false);
