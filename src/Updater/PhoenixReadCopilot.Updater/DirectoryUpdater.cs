namespace PhoenixReadCopilot.Updater;

/// <summary>
/// Self-updater for apps distributed via a local directory or network share. Lists the
/// directory and treats files matching the configured convention (per
/// <see cref="UpdaterOptions"/>; the default is <c>{appName}-{version}-{rid}</c>) as
/// releases — other files are ignored. Optionally verifies a sidecar
/// <c>{binary}.sha256</c> checksum, then hands off to the new binary to replace the
/// running one in place. Useful for LAN/offline distribution and air-gapped rollouts —
/// no HTTP involved.
/// <para>
/// A sidecar's contents may be a bare hash or the leading <c>sha256sum</c>-style
/// <c>&lt;hash&gt;&#160;&#160;filename</c> token.
/// </para>
/// </summary>
public sealed class DirectoryUpdater : Updater
{
    private const string ChecksumExtension = ".sha256";

    private readonly string _directory;

    /// <param name="directory">Directory or share holding the release binaries.</param>
    /// <param name="options">Identity, version, platform, and swap settings.</param>
    public DirectoryUpdater(string directory, UpdaterOptions options)
        : base(options)
    {
        _directory = directory;
    }

    internal override async Task<IReadOnlyList<SourceAsset>> GetAssetsAsync(CancellationToken ct)
    {
        if (!Directory.Exists(_directory))
        {
            return [];
        }

        List<SourceAsset> assets = [];
        foreach (var path in Directory.EnumerateFiles(_directory))
        {
            ct.ThrowIfCancellationRequested();

            var fileName = Path.GetFileName(path);
            if (fileName.EndsWith(ChecksumExtension, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var sha256 = await ReadSidecarChecksumAsync(path, ct).ConfigureAwait(false);
            assets.Add(new SourceAsset(fileName, path, sha256, new FileInfo(path).Length));
        }

        return assets;
    }

    internal override Task<Stream> OpenAssetAsync(SourceAsset asset, CancellationToken ct) =>
        Task.FromResult<Stream>(File.OpenRead(asset.Location));

    private static async Task<string?> ReadSidecarChecksumAsync(
        string assetPath,
        CancellationToken ct
    )
    {
        var sidecar = assetPath + ChecksumExtension;
        if (!File.Exists(sidecar))
        {
            return null;
        }

        var text = (await File.ReadAllTextAsync(sidecar, ct).ConfigureAwait(false)).Trim();
        if (text.Length == 0)
        {
            return null;
        }

        // Accept a bare hash or the "<hash>  filename" sha256sum format.
        var space = text.IndexOfAny([' ', '\t']);
        return space > 0 ? text[..space] : text;
    }
}
