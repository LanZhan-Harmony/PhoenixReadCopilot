namespace PhoenixReadCopilot.Updater;

/// <summary>
/// 更新操作的结果状态.
/// </summary>
public enum UpdateOutcome
{
    /// <summary>已经是最新版本.</summary>
    UpToDate,

    /// <summary>发现新版本可用.</summary>
    UpdateAvailable,

    /// <summary>新版本已下载并准备就绪，等待应用退出后替换.</summary>
    Staged,

    /// <summary>更新失败（网络、校验或其他错误）.</summary>
    Failed,
}
