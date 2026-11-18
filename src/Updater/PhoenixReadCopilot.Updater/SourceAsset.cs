namespace PhoenixReadCopilot.Updater;

/// <summary>
/// 表示一个可下载的发行版资源.
/// </summary>
/// <param name="Name">资源文件名 (如 PhoenixReadCopilot_0.1.0.0_x64.zip).</param>
/// <param name="DownloadUrl">原始 GitHub 下载地址.</param>
/// <param name="TagName">所属 Release 的 tag 名称 (如 Pre-Release-0.1.0.0).</param>
/// <param name="Size">资源文件大小 (字节).</param>
/// <param name="IsPrerelease">是否为预发布版本.</param>
public sealed record SourceAsset(
    string Name,
    string DownloadUrl,
    string TagName,
    long Size,
    bool IsPrerelease = false
);
