namespace PhoenixReadCopilot.Updater;

public enum UpdateOutcome
{
    /// <summary>Already on the latest version (or newer).</summary>
    UpToDate,

    /// <summary>A newer build was downloaded, validated, and handed off; the caller should now exit.</summary>
    Staged,

    /// <summary>The running process is not a self-contained single file, so it cannot replace itself.</summary>
    NotSelfContained,

    /// <summary>The update could not be completed (network, checksum, validation, ...).</summary>
    Failed,
}
