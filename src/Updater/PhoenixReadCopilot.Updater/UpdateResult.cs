namespace PhoenixReadCopilot.Updater;

/// <summary>
/// 更新检查或执行更新后的结果.
/// </summary>
public sealed record UpdateResult(
    UpdateOutcome Outcome,
    Version? Version = null,
    string? Message = null
);
