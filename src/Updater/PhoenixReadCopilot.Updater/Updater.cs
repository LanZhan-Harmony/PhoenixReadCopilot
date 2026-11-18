using System.Diagnostics;
using System.IO.Compression;

namespace PhoenixReadCopilot.Updater;

/// <summary>
/// 更新应用引擎：负责将下载的 zip 包解压并替换当前程序目录.
/// <para>
/// 更新流程:
/// 1. 下载 zip 到临时目录
/// 2. 解压 zip 到临时目录
/// 3. 启动新版本的 exe 执行替换操作 (handoff)
/// 4. 旧进程退出后，新进程将临时目录的文件复制到目标目录，同时保留 AppData 文件夹
/// 5. 重新启动应用
/// </para>
/// </summary>
public static class UpdateApplier
{
    /// <summary>
    /// 要在目录替换时保留（不删除）的文件夹名称.
    /// exe 所在目录的 AppData 是用户数据存储路径，更新时必须保留.
    /// </summary>
    private static readonly HashSet<string> PreservedDirectories =
    [
        with(StringComparer.OrdinalIgnoreCase),
        "AppData",
    ];

    // 命令行参数常量
    public const string HandoffVerb = "apply-update";
    public const string DestOption = "--dest";
    public const string PidOption = "--pid";
    public const string SourceDirOption = "--source-dir";
    public const string RelaunchOption = "--relaunch";

    /// <summary>
    /// 下载并准备更新，然后启动新版本 exe 执行替换.
    /// </summary>
    /// <param name="updater">GitHub 更新器实例.</param>
    /// <param name="release">要更新到的版本.</param>
    /// <param name="targetDirectory">当前应用程序目录 (exe 所在目录).</param>
    /// <param name="progress">下载进度回调.</param>
    /// <param name="log">日志输出.</param>
    /// <param name="ct">取消令牌.</param>
    /// <returns>更新结果.</returns>
    public static async Task<UpdateResult> DownloadAndStageAsync(
        GitHubUpdater updater,
        AppRelease release,
        string targetDirectory,
        IProgress<(long Downloaded, long Total)>? progress = null,
        TextWriter? log = null,
        CancellationToken ct = default
    )
    {
        log ??= Console.Out;
        targetDirectory = Path.GetFullPath(targetDirectory);

        string exePath = Environment.ProcessPath ?? string.Empty;
        if (string.IsNullOrEmpty(exePath))
        {
            log.WriteLine("无法确定当前可执行文件路径.");
            return new UpdateResult(
                UpdateOutcome.Failed,
                release.Version,
                "无法确定当前可执行文件路径."
            );
        }

        // 确认 exe 在目标目录内
        string relExe = Path.GetRelativePath(targetDirectory, Path.GetFullPath(exePath));
        if (relExe.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relExe))
        {
            log.WriteLine("当前可执行文件不在目标目录内.");
            return new UpdateResult(
                UpdateOutcome.Failed,
                release.Version,
                "当前可执行文件不在目标目录内."
            );
        }

        // 1. 下载 zip 到临时目录
        string stagingDir = Path.Combine(
            Path.GetTempPath(),
            $"PhoenixReadCopilot-update-{Path.GetRandomFileName()}"
        );
        Directory.CreateDirectory(stagingDir);

        string zipPath = Path.Combine(stagingDir, release.Asset.Name);
        log.WriteLine($"下载更新包到 {zipPath}...");

        bool downloaded = await updater
            .DownloadUpdateAsync(release, zipPath, progress, ct)
            .ConfigureAwait(false);

        if (!downloaded)
        {
            return new UpdateResult(UpdateOutcome.Failed, release.Version, "下载更新包失败.");
        }

        // 2. 解压
        string extractDir = Path.Combine(stagingDir, "extracted");
        try
        {
            log.WriteLine("解压更新包...");
            ZipFile.ExtractToDirectory(zipPath, extractDir);
        }
        catch (Exception ex)
        {
            log.WriteLine($"解压失败: {ex.Message}");
            return new UpdateResult(
                UpdateOutcome.Failed,
                release.Version,
                $"解压失败: {ex.Message}"
            );
        }

        // 检查解压结果: 如果解压出的是单一目录，则使用该目录作为源
        string[] dirs = Directory.GetDirectories(extractDir);
        string[] files = Directory.GetFiles(extractDir);
        string sourceDir = dirs.Length == 1 && files.Length == 0 ? dirs[0] : extractDir;

        // 3. 验证新版本中存在对应的 exe
        string stagedExe = Path.Combine(sourceDir, relExe);
        if (!File.Exists(stagedExe))
        {
            log.WriteLine($"新版本中未找到可执行文件: {relExe}");
            return new UpdateResult(
                UpdateOutcome.Failed,
                release.Version,
                "新版本中未找到可执行文件."
            );
        }

        // 4. 启动新版本 exe 执行替换
        log.WriteLine($"启动新版本进行替换...");
        ProcessStartInfo psi = new()
        {
            FileName = stagedExe,
            ArgumentList =
            {
                HandoffVerb,
                DestOption,
                targetDirectory,
                PidOption,
                Environment.ProcessId.ToString(),
                SourceDirOption,
                sourceDir,
                RelaunchOption,
            },
        };

        if (Process.Start(psi) is null)
        {
            return new UpdateResult(UpdateOutcome.Failed, release.Version, "无法启动更新替换进程.");
        }

        return new UpdateResult(UpdateOutcome.Staged, release.Version);
    }

    /// <summary>
    /// Handoff 端: 由新版本 exe 调用，等待旧进程退出后替换目标目录.
    /// 保留 <see cref="PreservedDirectories"/> 中列出的文件夹.
    /// </summary>
    /// <param name="destDir">目标应用程序目录.</param>
    /// <param name="sourceDir">解压后的新版本目录.</param>
    /// <param name="oldPid">旧进程 PID (等待其退出).</param>
    /// <param name="relaunch">替换完成后是否重新启动应用.</param>
    /// <param name="log">日志输出.</param>
    /// <returns>退出代码 (0=成功, 1=失败).</returns>
    public static int ApplySwap(
        string destDir,
        string sourceDir,
        int oldPid,
        bool relaunch = true,
        TextWriter? log = null
    )
    {
        log ??= Console.Out;
        destDir = Path.GetFullPath(destDir);
        sourceDir = Path.GetFullPath(sourceDir);

        // 确定可执行文件的相对路径（用于替换后重新启动）
        string? processPath = Environment.ProcessPath;
        string? relExe = null;
        if (!string.IsNullOrEmpty(processPath))
        {
            string rel = Path.GetRelativePath(sourceDir, Path.GetFullPath(processPath));
            if (
                !Path.IsPathRooted(rel)
                && !rel.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                && rel != ".."
            )
            {
                relExe = rel;
            }
        }

        // 等待旧进程退出
        WaitForExit(oldPid, log);

        try
        {
            // 删除目标目录中的文件和非保留目录
            if (Directory.Exists(destDir))
            {
                log.WriteLine($"清理目标目录 (保留 {string.Join(", ", PreservedDirectories)})...");

                // 删除目标目录中所有文件
                foreach (string file in Directory.GetFiles(destDir))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        log.WriteLine($"删除文件失败 {Path.GetFileName(file)}: {ex.Message}");
                    }
                }

                // 删除非保留的子目录
                foreach (string dir in Directory.GetDirectories(destDir))
                {
                    string dirName = Path.GetFileName(dir);
                    if (PreservedDirectories.Contains(dirName))
                    {
                        log.WriteLine($"保留目录: {dirName}");
                        continue;
                    }

                    try
                    {
                        Directory.Delete(dir, recursive: true);
                    }
                    catch (Exception ex)
                    {
                        log.WriteLine($"删除目录失败 {dirName}: {ex.Message}");
                    }
                }
            }
            else
            {
                Directory.CreateDirectory(destDir);
            }

            // 复制新版本文件到目标目录 (不覆盖保留目录)
            log.WriteLine("复制新版本文件...");
            CopyDirectorySelective(sourceDir, destDir);

            log.WriteLine($"更新完成: {destDir}");
        }
        catch (Exception ex)
        {
            log.WriteLine($"替换失败: {ex.Message}");
            return 1;
        }

        // 重新启动
        if (relaunch && relExe is not null)
        {
            string launchExe = Path.Combine(destDir, relExe);
            if (File.Exists(launchExe))
            {
                log.WriteLine("重新启动应用...");
                Process.Start(new ProcessStartInfo { FileName = launchExe });
            }
        }

        return 0;
    }

    /// <summary>
    /// 递归复制目录，但跳过保留目录 (如 AppData).
    /// </summary>
    private static void CopyDirectorySelective(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        // 复制文件
        foreach (string file in Directory.GetFiles(sourceDir))
        {
            string destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, destFile, overwrite: true);
        }

        // 递归复制子目录 (跳过保留目录)
        foreach (string dir in Directory.GetDirectories(sourceDir))
        {
            string dirName = Path.GetFileName(dir);

            // 如果目标目录中已有同名保留目录，跳过
            if (PreservedDirectories.Contains(dirName))
            {
                string destSubDir = Path.Combine(destDir, dirName);
                if (Directory.Exists(destSubDir))
                {
                    continue;
                }
            }

            CopyDirectorySelective(dir, Path.Combine(destDir, dirName));
        }
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
            log.WriteLine($"等待旧进程 (PID {oldPid}) 退出...");
            if (!old.WaitForExit(30_000))
            {
                log.WriteLine("旧进程超时未退出，尝试强制替换.");
            }
        }
        catch (ArgumentException)
        {
            // 进程已退出
        }
    }
}
