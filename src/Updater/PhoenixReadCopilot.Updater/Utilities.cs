using System.Reflection;
using System.Security.Cryptography;

namespace PhoenixReadCopilot.Updater;

internal static class Utilities
{
    /// <summary>Full path of the running executable.</summary>
    public static string? ProcessPath => Environment.ProcessPath;

    /// <summary>
    /// True when running as a self-contained single-file app (the case the swap
    /// supports). Single-file/AOT apps report an empty assembly location, which is
    /// exactly the signal we want here.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(
        "SingleFile",
        "IL3000",
        Justification = "An empty Location is precisely the single-file signal we test for."
    )]
    public static bool IsSingleFile() =>
        string.IsNullOrEmpty(Assembly.GetEntryAssembly()?.Location);

    public static void MakeExecutable(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        UnixFileMode mode = File.GetUnixFileMode(path);
        mode |= UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;
        File.SetUnixFileMode(path, mode);
    }

    /// <summary>
    /// Recursively copy a directory tree, preserving Unix file modes (so executable
    /// bits inside an app bundle survive the copy). Overwrites existing files.
    /// </summary>
    public static void CopyDirectory(string source, string dest)
    {
        Directory.CreateDirectory(dest);
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(dest, File.GetUnixFileMode(source));
        }

        foreach (var dir in Directory.GetDirectories(source))
        {
            CopyDirectory(dir, Path.Combine(dest, Path.GetFileName(dir)));
        }

        foreach (var file in Directory.GetFiles(source))
        {
            var target = Path.Combine(dest, Path.GetFileName(file));
            File.Copy(file, target, overwrite: true);
            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(target, File.GetUnixFileMode(file));
            }
        }
    }

    public static async Task<string> ComputeSha256Async(string path, CancellationToken ct)
    {
        await using FileStream stream = File.OpenRead(path);
        var hash = await SHA256.HashDataAsync(stream, ct).ConfigureAwait(false);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
