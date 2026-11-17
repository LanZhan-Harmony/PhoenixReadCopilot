using System.Runtime.InteropServices;
using Semver;

namespace PhoenixReadCopilot.Updater;

/// <summary>
/// Everything an updater needs: the app's identity and version, the platform to
/// target, how asset names map to releases, and how to stage the swap. Build one up,
/// hand it to a concrete updater (e.g. <c>new GitHubUpdater(owner, repo, options)</c>),
/// then call <c>UpdateAsync</c> (or <c>FetchAsync</c> + <c>ApplyAsync</c>).
/// </summary>
public sealed record UpdaterOptions
{
    /// <summary>
    /// Application name for the default <c>{appName}-{version}-{rid}</c> naming
    /// convention. Ignored when a custom <see cref="Parser"/> is supplied.
    /// </summary>
    public required string AppName { get; init; }

    /// <summary>
    /// The version the app is currently running. The updater never fetches this for
    /// you; you own it. A release is "newer" when its version has higher precedence.
    /// </summary>
    public required SemVersion CurrentVersion { get; init; }

    /// <summary>
    /// Target runtime identifier (e.g. <c>osx-arm64</c>): the suffix the default
    /// convention selects on, and what makes the otherwise-ambiguous
    /// <c>{version}-{rid}</c> tail splittable. Defaults to the running platform's
    /// <see cref="RuntimeInformation.RuntimeIdentifier"/>.
    /// </summary>
    public string Rid { get; init; } = RuntimeInformation.RuntimeIdentifier;

    /// <summary>
    /// Optional override for how a raw asset name maps to <c>(version, rid)</c>. When
    /// omitted the default <c>{appName}-{version}-{rid}</c> convention is used. Only
    /// assets whose mapped rid equals <see cref="Rid"/> are kept.
    /// </summary>
    public AssetNameParser? Parser { get; init; }

    /// <summary>
    /// Optional predicate restricting which releases are considered (e.g.
    /// <c>r =&gt; !r.IsPrerelease</c> to ignore prereleases). When <c>null</c> every
    /// release for the platform is a candidate.
    /// </summary>
    public Func<Release, bool>? ReleaseFilter { get; init; }

    /// <summary>Path of the binary to replace. Defaults to the running executable.</summary>
    public string? TargetPath { get; init; }

    /// <summary>
    /// Opt into <b>directory (multi-file) updates</b>. When set, the release asset is
    /// treated as a <c>.zip</c> or <c>.tar.gz</c>/<c>.tgz</c> archive containing a single
    /// top-level directory, and the whole tree at this path is replaced in place — for
    /// apps that are not a single file (e.g. a macOS <c>.app</c> bundle, or a binary
    /// shipping sidecar native assets). The running executable (<see cref="TargetPath"/>
    /// or the current process) must live inside this directory; its location relative to
    /// the root is reused to launch the staged build and to relaunch after the swap. When
    /// <c>null</c> (the default) the updater swaps a single file.
    /// </summary>
    public string? TargetDirectory { get; init; }

    /// <summary>
    /// Arguments used to smoke-test a freshly downloaded binary, which must exit 0.
    /// Validation is <b>opt-in</b>: when <c>null</c> or empty (the default) the
    /// downloaded binary is not executed before being staged. Set this only if your
    /// binary supports the given arguments and exits 0 on success (e.g.
    /// <c>["--version"]</c>) — there is no universally safe probe to assume.
    /// </summary>
    public IReadOnlyList<string>? ValidateArgs { get; init; }

    /// <summary>When true, the handed-off process relaunches the app after swapping.</summary>
    public bool Relaunch { get; init; }

    /// <summary>Allow updating even when not deployed as a single file (e.g. for tests).</summary>
    public bool AllowNonSingleFile { get; init; }

    public TextWriter Log { get; init; } = Console.Out;
}
