using Semver;

namespace PhoenixReadCopilot.Updater;

/// <summary>A release for the configured platform, parsed from a source's assets.</summary>
/// <param name="Version">The release version, parsed from the asset's name.</param>
/// <param name="Asset">The raw asset for the configured <see cref="UpdaterOptions.Rid"/>.</param>
/// <param name="IsPrerelease">
/// True when the publisher marked this a prerelease (GitHub's <c>prerelease</c> flag,
/// or a SemVer prerelease suffix). Surfaced so a consumer can choose to skip
/// prereleases via <see cref="UpdaterOptions.ReleaseFilter"/>.
/// </param>
public sealed record Release(SemVersion Version, SourceAsset Asset, bool IsPrerelease = false);
