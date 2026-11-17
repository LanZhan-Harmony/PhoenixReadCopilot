using Semver;

namespace PhoenixReadCopilot.Updater;

/// <summary>
/// Maps a source's raw asset name (a file name, a GitHub asset name, ...) to the
/// <c>(Version, Rid)</c> it represents, or <c>null</c> to skip it. The
/// <see cref="Updater"/> runs every raw asset through one of these, keeps those whose
/// <c>Rid</c> matches <see cref="UpdaterOptions.Rid"/>, and groups the rest into
/// <see cref="Release"/>s by <c>Version</c>. Supply one via
/// <see cref="UpdaterOptions.Parser"/> for any scheme other than the default
/// <c>{appName}-{version}-{rid}</c>.
/// </summary>
public delegate (SemVersion Version, string Rid)? AssetNameParser(string name);

/// <summary>
/// Naming logic the <see cref="Updater"/> applies to a source's raw assets. The
/// default convention is <c>{appName}-{version}-{rid}</c> (e.g.
/// <c>myapp-1.2.3-osx-arm64</c>).
/// <para>
/// Because both the version (a SemVer prerelease such as <c>1.2.3-beta.1</c>) and
/// the rid (e.g. <c>osx-arm64</c>) can contain <c>-</c>, a name cannot be split into
/// its parts on its own. The default parser resolves this by knowing the target rid
/// up front (<see cref="UpdaterOptions.Rid"/>, defaulting to the running platform): it
/// matches only names ending in that rid and treats everything between the known
/// prefix and suffix as the version.
/// </para>
/// </summary>
internal static class AssetNaming
{
    /// <summary>
    /// Archive extensions recognized by the default convention. A multi-file release
    /// ships as one of these (e.g. a macOS <c>.app</c> bundle zipped to
    /// <c>myapp-1.2.3-osx-arm64.zip</c>); the extension is stripped before the
    /// <c>{appName}-{version}-{rid}</c> match so archives and bare binaries share a
    /// naming scheme. Longest/compound extensions come first.
    /// </summary>
    private static readonly string[] ArchiveExtensions = [".tar.gz", ".tgz", ".zip"];

    /// <summary>
    /// The default <c>{appName}-{version}-{rid}</c> parser. Any known archive
    /// extension is stripped first, then the known <c>{appName}-</c> prefix and
    /// <c>-{rid}</c> suffix are removed and whatever remains — dashes and all — is
    /// parsed as the version. Names that don't fit, or whose version segment doesn't
    /// parse, are skipped.
    /// </summary>
    public static AssetNameParser DefaultParser(string appName, string rid)
    {
        var prefix = appName + "-";
        var suffix = "-" + rid;
        return rawName =>
        {
            var name = StripArchiveExtension(rawName);

            // Must contain at least one version character between prefix and suffix.
            if (name.Length <= prefix.Length + suffix.Length)
            {
                return null;
            }

            if (
                !name.StartsWith(prefix, StringComparison.Ordinal)
                || !name.EndsWith(suffix, StringComparison.Ordinal)
            )
            {
                return null;
            }

            var versionText = name[prefix.Length..^suffix.Length];
            return SemVersion.TryParse(versionText, SemVersionStyles.Any, out SemVersion? version)
                ? (version, rid)
                : null;
        };
    }

    /// <summary>Drop a recognized archive extension (case-insensitive), if present.</summary>
    private static string StripArchiveExtension(string name)
    {
        foreach (var ext in ArchiveExtensions)
        {
            if (name.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
            {
                return name[..^ext.Length];
            }
        }
        return name;
    }

    /// <summary>
    /// Parse each raw asset's name, drop the ones the parser rejects or whose rid is
    /// not <paramref name="rid"/>, and group the rest into <see cref="Release"/>s by
    /// version (the first asset seen for a version wins).
    /// </summary>
    public static IReadOnlyList<Release> ToReleases(
        IEnumerable<SourceAsset> assets,
        AssetNameParser parse,
        string rid
    )
    {
        Dictionary<SemVersion, Release> byVersion = [];
        foreach (SourceAsset asset in assets)
        {
            if (parse(asset.Name) is not { } parsed)
            {
                continue;
            }

            (SemVersion? version, string? parsedRid) = parsed;
            if (!string.Equals(parsedRid, rid, StringComparison.Ordinal))
            {
                continue;
            }

            if (byVersion.ContainsKey(version))
            {
                continue;
            }

            byVersion[version] = new Release(
                version,
                asset,
                version.IsPrerelease || asset.IsPrerelease
            );
        }

        return [.. byVersion.Values];
    }
}
