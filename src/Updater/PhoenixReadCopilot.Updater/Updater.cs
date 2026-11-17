using System.Diagnostics;
using System.Formats.Tar;
using System.IO.Compression;
using Semver;

namespace PhoenixReadCopilot.Updater;

/// <summary>
/// Source-agnostic self-update engine, modeled on dnvm. A concrete updater
/// (<see cref="GitHubUpdater"/>, <see cref="DirectoryUpdater"/>) supplies just two
/// things — how to list a source's raw assets and how to open one's bytes — and this
/// base does the rest: turn asset names into releases for the configured platform,
/// pick the newest, download and verify it, then hand off to the new binary which
/// replaces the old one in place (a two-process swap so a running executable can
/// update itself, including on Windows).
/// <para>
/// The caller owns its current version (<see cref="UpdaterOptions.CurrentVersion"/>);
/// the engine owns naming, platform selection, and the "newest wins" comparison.
/// </para>
/// </summary>
public abstract class Updater
{
    // Wire contract for the handoff (new-process) side. The host app registers a
    // command/handler with these exact names; keeping them here makes this the
    // single source of truth shared by both processes.
    public const string HandoffVerb = "apply-update";
    public const string DestOption = "--dest";
    public const string PidOption = "--pid";
    public const string RelaunchOption = "--relaunch";

    /// <summary>
    /// Handoff flag carrying the staged source directory for a directory (multi-file)
    /// update. Present only when <see cref="UpdaterOptions.TargetDirectory"/> is set;
    /// its absence selects the single-file swap. The host's <see cref="HandoffVerb"/>
    /// handler should parse it and forward it to <see cref="ApplySwap"/>.
    /// </summary>
    public const string SourceDirOption = "--source-dir";

    private readonly UpdaterOptions _options;
    private readonly AssetNameParser _parse;
    private readonly TextWriter _log;

    protected Updater(UpdaterOptions options)
    {
        _options = options;
        _parse = options.Parser ?? AssetNaming.DefaultParser(options.AppName, options.Rid);
        _log = options.Log;
    }

    /// <summary>
    /// List every raw artifact this source can see, with whatever metadata it knows
    /// (location, integrity hash, size, prerelease hint). Returns an empty list if the
    /// source could not be reached or parsed. Order is not significant.
    /// </summary>
    internal abstract Task<IReadOnlyList<SourceAsset>> GetAssetsAsync(CancellationToken ct);

    /// <summary>Open a read stream over an asset's bytes for downloading.</summary>
    internal abstract Task<Stream> OpenAssetAsync(SourceAsset asset, CancellationToken ct);

    /// <summary>
    /// List the releases available for the configured platform, with asset names
    /// parsed into versions and filtered to <see cref="UpdaterOptions.Rid"/>. A
    /// naming/selection testing seam; applies no version comparison and no
    /// <see cref="UpdaterOptions.ReleaseFilter"/>.
    /// </summary>
    internal async Task<IReadOnlyList<Release>> GetReleasesAsync(CancellationToken ct = default)
    {
        IReadOnlyList<SourceAsset> assets = await GetAssetsAsync(ct).ConfigureAwait(false);
        return AssetNaming.ToReleases(assets, _parse, _options.Rid);
    }

    /// <summary>
    /// Resolve the exact release this updater would move to: the newest release for the
    /// configured platform (after <see cref="UpdaterOptions.ReleaseFilter"/>) whose
    /// version is newer than <see cref="UpdaterOptions.CurrentVersion"/>. Returns
    /// <c>null</c> when nothing applies — already up to date, or the source has no build
    /// for this platform. This is the network round-trip; hand the result to
    /// <see cref="ApplyAsync"/> to download and stage it (or just call
    /// <see cref="UpdateAsync"/>, which does both).
    /// </summary>
    public async Task<Release?> FetchAsync(CancellationToken ct = default)
    {
        SemVersion current = _options.CurrentVersion;
        _log.WriteLine($"Checking for updates (current {current})...");
        IReadOnlyList<SourceAsset> assets = await GetAssetsAsync(ct).ConfigureAwait(false);
        IReadOnlyList<Release> releases = AssetNaming.ToReleases(assets, _parse, _options.Rid);
        Release? newest = Newest(releases, _options.ReleaseFilter);
        if (newest is null)
        {
            if (assets.Count > 0)
            {
                _log.WriteLine($"Source has builds, but none for {_options.Rid}.");
            }
            else
            {
                _log.WriteLine("Update source returned no release information.");
            }

            return null;
        }

        if (newest.Version.ComparePrecedenceTo(current) <= 0)
        {
            _log.WriteLine($"Up to date (latest {newest.Version}).");
            return null;
        }

        _log.WriteLine($"Update available: {newest.Version}");
        return newest;
    }

    /// <summary>
    /// Download, validate, and stage a release resolved by <see cref="FetchAsync"/>,
    /// then hand off to the new binary. Applies exactly the given release — it does no
    /// version comparison of its own.
    /// </summary>
    public async Task<UpdateResult> ApplyAsync(Release release, CancellationToken ct = default)
    {
        if (!_options.AllowNonSingleFile && !Utilities.IsSingleFile())
        {
            _log.WriteLine(
                "Cannot self-update: not deployed as a single file (e.g. running via 'dotnet run')."
            );
            return new UpdateResult(UpdateOutcome.NotSelfContained, release.Version);
        }

        return _options.TargetDirectory is { Length: > 0 } targetDir
            ? await ApplyDirectoryAsync(release, targetDir, ct).ConfigureAwait(false)
            : await ApplyFileAsync(release, ct).ConfigureAwait(false);
    }

    /// <summary>Single-file swap: download the binary, validate it, hand off to replace the target file.</summary>
    private async Task<UpdateResult> ApplyFileAsync(Release release, CancellationToken ct)
    {
        var target = _options.TargetPath ?? Utilities.ProcessPath;
        if (string.IsNullOrEmpty(target))
        {
            _log.WriteLine("Cannot self-update: unable to determine the target executable path.");
            return new UpdateResult(UpdateOutcome.Failed, release.Version, "Unknown target path.");
        }

        var staged = await DownloadAndValidateAsync(release.Asset, target, ct)
            .ConfigureAwait(false);
        if (staged is null)
        {
            return new UpdateResult(
                UpdateOutcome.Failed,
                release.Version,
                "Download or validation failed."
            );
        }

        if (!LaunchHandoff(staged, target))
        {
            return new UpdateResult(
                UpdateOutcome.Failed,
                release.Version,
                "Could not launch the handoff process."
            );
        }

        return new UpdateResult(UpdateOutcome.Staged, release.Version);
    }

    /// <summary>
    /// Directory (multi-file) swap: download the archive, extract it, validate the
    /// staged executable, then hand off to replace the whole target directory tree.
    /// </summary>
    private async Task<UpdateResult> ApplyDirectoryAsync(
        Release release,
        string targetDir,
        CancellationToken ct
    )
    {
        targetDir = Path.GetFullPath(targetDir);
        var exePath = _options.TargetPath ?? Utilities.ProcessPath;
        if (string.IsNullOrEmpty(exePath))
        {
            _log.WriteLine("Cannot self-update: unable to determine the target executable path.");
            return new UpdateResult(UpdateOutcome.Failed, release.Version, "Unknown target path.");
        }

        // The executable must live under the directory we are going to replace; its
        // relative location is how we find the staged build and relaunch the new one.
        var relExe = Path.GetRelativePath(targetDir, Path.GetFullPath(exePath));
        if (relExe.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relExe))
        {
            _log.WriteLine(
                $"Cannot self-update: the running executable is not inside TargetDirectory ({targetDir})."
            );
            return new UpdateResult(
                UpdateOutcome.Failed,
                release.Version,
                "Executable is outside the target directory."
            );
        }

        var stagedDir = await DownloadAndExtractAsync(release.Asset, ct).ConfigureAwait(false);
        if (stagedDir is null)
        {
            return new UpdateResult(
                UpdateOutcome.Failed,
                release.Version,
                "Download or extraction failed."
            );
        }

        var stagedExe = Path.Combine(stagedDir, relExe);
        if (!File.Exists(stagedExe))
        {
            _log.WriteLine($"Staged build does not contain the expected executable ({relExe}).");
            return new UpdateResult(
                UpdateOutcome.Failed,
                release.Version,
                "Staged build is missing the executable."
            );
        }
        if (!OperatingSystem.IsWindows())
        {
            Utilities.MakeExecutable(stagedExe);
        }

        if (!await ValidateAsync(stagedExe, ct).ConfigureAwait(false))
        {
            return new UpdateResult(UpdateOutcome.Failed, release.Version, "Validation failed.");
        }

        if (!LaunchHandoff(stagedExe, targetDir, stagedDir))
        {
            return new UpdateResult(
                UpdateOutcome.Failed,
                release.Version,
                "Could not launch the handoff process."
            );
        }

        return new UpdateResult(UpdateOutcome.Staged, release.Version);
    }

    /// <summary>
    /// Shorthand for <see cref="FetchAsync"/> followed by <see cref="ApplyAsync"/>:
    /// resolve the release to move to and, if there is one, download, validate, stage,
    /// and hand off. Reports <see cref="UpdateOutcome.UpToDate"/> when nothing applies.
    /// </summary>
    public async Task<UpdateResult> UpdateAsync(CancellationToken ct = default)
    {
        Release? release = await FetchAsync(ct).ConfigureAwait(false);
        if (release is null)
        {
            return new UpdateResult(UpdateOutcome.UpToDate);
        }

        return await ApplyAsync(release, ct).ConfigureAwait(false);
    }

    private static Release? Newest(IReadOnlyList<Release> releases, Func<Release, bool>? filter)
    {
        Release? best = null;
        foreach (Release release in releases)
        {
            if (filter is not null && !filter(release))
            {
                continue;
            }

            if (best is null || release.Version.ComparePrecedenceTo(best.Version) > 0)
            {
                best = release;
            }
        }
        return best;
    }

    private async Task<string?> DownloadAndValidateAsync(
        SourceAsset asset,
        string target,
        CancellationToken ct
    )
    {
        var tempDir = NewStagingDir();
        // Name the staged file after the target so the swapped-in binary keeps its name.
        var staged = Path.Combine(tempDir, Path.GetFileName(target));

        if (!await DownloadToFileAsync(asset, staged, ct).ConfigureAwait(false))
        {
            return null;
        }

        if (!await VerifyChecksumAsync(staged, asset, ct).ConfigureAwait(false))
        {
            return null;
        }

        if (!OperatingSystem.IsWindows())
        {
            Utilities.MakeExecutable(staged);
        }

        if (!await ValidateAsync(staged, ct).ConfigureAwait(false))
        {
            return null;
        }

        return staged;
    }

    /// <summary>
    /// Download the release archive, verify its checksum, and extract it to a staging
    /// directory. Returns the staged build root — the single top-level directory inside
    /// the archive (e.g. <c>Bower.app</c>), or the extraction directory itself when the
    /// archive has no single wrapping directory — or <c>null</c> on failure.
    /// </summary>
    private async Task<string?> DownloadAndExtractAsync(SourceAsset asset, CancellationToken ct)
    {
        var tempDir = NewStagingDir();
        // Keep the asset's own file name so extraction can dispatch on its extension.
        var fileName = Path.GetFileName(asset.Name);
        if (string.IsNullOrEmpty(fileName))
        {
            fileName = "download.zip";
        }

        var archive = Path.Combine(tempDir, fileName);

        if (!await DownloadToFileAsync(asset, archive, ct).ConfigureAwait(false))
        {
            return null;
        }

        if (!await VerifyChecksumAsync(archive, asset, ct).ConfigureAwait(false))
        {
            return null;
        }

        var extractDir = Path.Combine(tempDir, "extracted");
        try
        {
            ExtractArchive(archive, extractDir);
        }
        catch (Exception e)
        {
            _log.WriteLine($"Extraction failed: {e.Message}");
            return null;
        }

        var dirs = Directory.GetDirectories(extractDir);
        var files = Directory.GetFiles(extractDir);
        return dirs.Length == 1 && files.Length == 0 ? dirs[0] : extractDir;
    }

    /// <summary>
    /// Extract a release archive to <paramref name="extractDir"/>, dispatching on the
    /// archive's file extension: <c>.tar.gz</c>/<c>.tgz</c> via gzip + tar, everything
    /// else as a <c>.zip</c>. Both restore Unix file permissions recorded in the archive
    /// (and tar restores symlinks), so executable bits inside a bundle survive the
    /// round-trip.
    /// </summary>
    internal static void ExtractArchive(string archivePath, string extractDir)
    {
        if (IsTarball(archivePath))
        {
            Directory.CreateDirectory(extractDir);
            using FileStream file = File.OpenRead(archivePath);
            using GZipStream gzip = new(file, CompressionMode.Decompress);
            TarFile.ExtractToDirectory(gzip, extractDir, overwriteFiles: true);
        }
        else
        {
            ZipFile.ExtractToDirectory(archivePath, extractDir);
        }
    }

    private static bool IsTarball(string name) =>
        name.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase)
        || name.EndsWith(".tgz", StringComparison.OrdinalIgnoreCase);

    private static string NewStagingDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "selfupdater-" + Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        return dir;
    }

    private async Task<bool> DownloadToFileAsync(
        SourceAsset asset,
        string path,
        CancellationToken ct
    )
    {
        _log.WriteLine($"Downloading {asset.Location}...");
        try
        {
            await using Stream src = await OpenAssetAsync(asset, ct).ConfigureAwait(false);
            await using FileStream file = new(
                path,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None
            );
            await src.CopyToAsync(file, ct).ConfigureAwait(false);
            return true;
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            _log.WriteLine($"Download failed: {e.Message}");
            return false;
        }
    }

    private async Task<bool> VerifyChecksumAsync(
        string path,
        SourceAsset asset,
        CancellationToken ct
    )
    {
        if (string.IsNullOrEmpty(asset.Sha256))
        {
            return true;
        }

        var actual = await Utilities.ComputeSha256Async(path, ct).ConfigureAwait(false);
        if (!actual.Equals(asset.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            _log.WriteLine($"Checksum mismatch: expected {asset.Sha256}, got {actual}.");
            return false;
        }
        _log.WriteLine("Checksum OK.");
        return true;
    }

    private async Task<bool> ValidateAsync(string path, CancellationToken ct)
    {
        // Validation is opt-in: with no args configured we do not execute the
        // freshly downloaded binary, since there is no universally safe probe.
        if (_options.ValidateArgs is not { Count: > 0 } validateArgs)
        {
            return true;
        }

        try
        {
            ProcessStartInfo psi = new()
            {
                FileName = path,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            foreach (var a in validateArgs)
            {
                psi.ArgumentList.Add(a);
            }

            using Process? proc = Process.Start(psi);
            if (proc is null)
            {
                _log.WriteLine("Could not start the downloaded binary for validation.");
                return false;
            }
            await proc.WaitForExitAsync(ct).ConfigureAwait(false);
            if (proc.ExitCode != 0)
            {
                _log.WriteLine($"Downloaded binary failed validation (exit {proc.ExitCode}).");
                return false;
            }
            return true;
        }
        catch (Exception e)
        {
            _log.WriteLine($"Validation error: {e.Message}");
            return false;
        }
    }

    private bool LaunchHandoff(string stagedPath, string target, string? sourceDir = null)
    {
        _log.WriteLine($"Staged update ready; handing off to replace {target}.");
        ProcessStartInfo psi = new()
        {
            FileName = stagedPath,
            ArgumentList =
            {
                HandoffVerb,
                DestOption,
                target,
                PidOption,
                Environment.ProcessId.ToString(),
            },
        };
        if (sourceDir is not null)
        {
            psi.ArgumentList.Add(SourceDirOption);
            psi.ArgumentList.Add(sourceDir);
        }
        if (_options.Relaunch)
        {
            psi.ArgumentList.Add(RelaunchOption);
        }

        return Process.Start(psi) is not null;
    }

    /// <summary>
    /// The handoff (new-process) side of the swap. Runs from the freshly staged build:
    /// waits for the previous process to exit, replaces the target in place, and
    /// optionally relaunches. Wire this up in your entry point under
    /// <see cref="HandoffVerb"/>.
    /// <para>
    /// When <paramref name="sourceDir"/> is <c>null</c> this performs a single-file
    /// swap (copy the running binary over <paramref name="destPath"/>). When set — for
    /// a directory (multi-file) update — <paramref name="destPath"/> is a directory and
    /// its whole tree is replaced with <paramref name="sourceDir"/>; relaunch targets
    /// the executable at the same relative location inside the swapped tree.
    /// </para>
    /// </summary>
    public static int ApplySwap(
        string destPath,
        int oldPid,
        IReadOnlyList<string>? relaunchArgs = null,
        string? sourceDir = null,
        TextWriter? log = null
    )
    {
        log ??= Console.Out;

        WaitForExit(oldPid, log);

        return sourceDir is { Length: > 0 }
            ? ApplyDirectorySwap(destPath, sourceDir, relaunchArgs, log)
            : ApplyFileSwap(destPath, relaunchArgs, log);
    }

    private static void WaitForExit(int oldPid, TextWriter log)
    {
        if (oldPid <= 0)
        {
            return;
        }

        try
        {
            using Process old = Process.GetProcessById(oldPid);
            log.WriteLine($"Waiting for previous process (pid {oldPid}) to exit...");
            if (!old.WaitForExit(30_000))
            {
                log.WriteLine("Previous process did not exit in time; attempting swap anyway.");
            }
        }
        catch (ArgumentException)
        {
            // Process already gone.
        }
    }

    private static int ApplyFileSwap(
        string destPath,
        IReadOnlyList<string>? relaunchArgs,
        TextWriter log
    )
    {
        var src = Utilities.ProcessPath;
        if (string.IsNullOrEmpty(src))
        {
            log.WriteLine("Cannot apply update: unknown source path.");
            return 1;
        }

        var backup = destPath + ".bak";
        try
        {
            if (File.Exists(destPath))
            {
                File.Move(destPath, backup, overwrite: true);
            }

            // Copy (rather than move) the still-running staged binary so the swap
            // works across volumes and the running image stays valid.
            File.Copy(src, destPath, overwrite: true);
            File.SetLastWriteTimeUtc(destPath, DateTime.UtcNow);
            if (!OperatingSystem.IsWindows())
            {
                Utilities.MakeExecutable(destPath);
            }

            if (File.Exists(backup))
            {
                try
                {
                    File.Delete(backup);
                }
                catch
                { /* a locked .bak on Windows is harmless; leave it for next run */
                }
            }
        }
        catch (Exception e)
        {
            log.WriteLine($"Swap failed: {e.Message}");
            if (File.Exists(backup) && !File.Exists(destPath))
            {
                try
                {
                    File.Move(backup, destPath);
                }
                catch
                { /* best effort */
                }
            }
            return 1;
        }

        log.WriteLine($"Updated in place: {destPath}");
        Relaunch(destPath, relaunchArgs, log);
        return 0;
    }

    private static int ApplyDirectorySwap(
        string destDir,
        string sourceDir,
        IReadOnlyList<string>? relaunchArgs,
        TextWriter log
    )
    {
        destDir = Path.GetFullPath(destDir);
        sourceDir = Path.GetFullPath(sourceDir);

        // The running process is expected to live inside sourceDir; reuse its relative
        // location to find the executable to relaunch inside the swapped-in tree. If it
        // does not (the relative path escapes sourceDir or stays rooted), we cannot know
        // what to launch in the new tree, so skip relaunch rather than touch a path
        // outside destDir.
        var processPath = Utilities.ProcessPath;
        string? relExe = null;
        if (!string.IsNullOrEmpty(processPath))
        {
            var rel = Path.GetRelativePath(sourceDir, Path.GetFullPath(processPath));
            if (
                !Path.IsPathRooted(rel)
                && rel != ".."
                && !rel.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            )
            {
                relExe = rel;
            }
        }

        var backup =
            destDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + ".bak";
        try
        {
            if (Directory.Exists(backup))
            {
                Directory.Delete(backup, recursive: true);
            }

            // Move the old tree aside, then copy the staged tree in. Copy (rather than
            // move) so the swap works across volumes and the still-running staged image
            // stays valid; the temp staging dir is reclaimed by the OS later.
            if (Directory.Exists(destDir))
            {
                Directory.Move(destDir, backup);
            }

            Utilities.CopyDirectory(sourceDir, destDir);

            if (Directory.Exists(backup))
            {
                try
                {
                    Directory.Delete(backup, recursive: true);
                }
                catch
                { /* leftover .bak is harmless; leave it for next run */
                }
            }
        }
        catch (Exception e)
        {
            log.WriteLine($"Swap failed: {e.Message}");
            if (Directory.Exists(backup) && !Directory.Exists(destDir))
            {
                try
                {
                    Directory.Move(backup, destDir);
                }
                catch
                { /* best effort */
                }
            }
            return 1;
        }

        log.WriteLine($"Updated in place: {destDir}");

        if (relExe is not null)
        {
            var exe = Path.Combine(destDir, relExe);
            if (!OperatingSystem.IsWindows() && File.Exists(exe))
            {
                Utilities.MakeExecutable(exe);
            }

            Relaunch(exe, relaunchArgs, log);
        }
        return 0;
    }

    private static void Relaunch(string exe, IReadOnlyList<string>? relaunchArgs, TextWriter log)
    {
        if (relaunchArgs is null)
        {
            return;
        }

        log.WriteLine("Relaunching...");
        ProcessStartInfo psi = new() { FileName = exe };
        foreach (var a in relaunchArgs)
        {
            psi.ArgumentList.Add(a);
        }

        Process.Start(psi);
    }
}
