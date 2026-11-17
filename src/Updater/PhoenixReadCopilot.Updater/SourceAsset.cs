namespace PhoenixReadCopilot.Updater;

/// <summary>
/// A raw, source-provided build artifact, identified by the publisher's own
/// <see cref="Name"/> (a GitHub asset file name, a directory file name, ...). The
/// name is interpreted only by the updater's naming convention (see
/// <see cref="AssetNameParser"/>). <see cref="Location"/> is opaque to the shared
/// engine and interpreted only by the updater that produced it (a URL for
/// <see cref="GitHubUpdater"/>, a file path for <see cref="DirectoryUpdater"/>).
/// </summary>
/// <param name="Name">The source's own asset name, fed to the naming convention.</param>
/// <param name="Location">Where the asset can be opened from (path or URL).</param>
/// <param name="Sha256">Optional integrity hash, if the source published one.</param>
/// <param name="Size">Optional asset size in bytes, if known.</param>
/// <param name="IsPrerelease">
/// A source-side prerelease signal (e.g. GitHub's <c>prerelease</c> flag) OR-ed into
/// the resulting release's <see cref="Release.IsPrerelease"/>, on top of whatever the
/// parsed version itself indicates.
/// </param>
public sealed record SourceAsset(
    string Name,
    string Location,
    string? Sha256 = null,
    long? Size = null,
    bool IsPrerelease = false
);
