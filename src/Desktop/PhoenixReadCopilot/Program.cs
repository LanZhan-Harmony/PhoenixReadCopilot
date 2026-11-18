using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using PhoenixReadCopilot.Updater;
using WinRT;

namespace PhoenixReadCopilot;

/// <summary>
/// 应用程序入口点.
/// </summary>
public static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        // 检查是否为更新替换进程 (apply-update)
        if (
            args.Length > 0
            && args[0].Equals(UpdateApplier.HandoffVerb, StringComparison.OrdinalIgnoreCase)
        )
        {
            HandleUpdateSwap(args);
            return;
        }

        ComWrappersSupport.InitializeComWrappers();
        Application.Start(
            (p) =>
            {
                DispatcherQueueSynchronizationContext context = new(
                    DispatcherQueue.GetForCurrentThread()
                );
                SynchronizationContext.SetSynchronizationContext(context);
                _ = new App();
            }
        );
    }

    private static void HandleUpdateSwap(string[] args)
    {
        string dest = string.Empty;
        int oldPid = 0;
        string sourceDir = string.Empty;
        bool relaunch = false;

        for (int i = 1; i < args.Length; i++)
        {
            if (
                args[i].Equals(UpdateApplier.DestOption, StringComparison.OrdinalIgnoreCase)
                && i + 1 < args.Length
            )
            {
                dest = args[++i];
            }
            else if (
                args[i].Equals(UpdateApplier.PidOption, StringComparison.OrdinalIgnoreCase)
                && i + 1 < args.Length
            )
            {
                _ = int.TryParse(args[++i], out oldPid);
            }
            else if (
                args[i].Equals(UpdateApplier.SourceDirOption, StringComparison.OrdinalIgnoreCase)
                && i + 1 < args.Length
            )
            {
                sourceDir = args[++i];
            }
            else if (
                args[i].Equals(UpdateApplier.RelaunchOption, StringComparison.OrdinalIgnoreCase)
            )
            {
                relaunch = true;
            }
        }

        if (!string.IsNullOrEmpty(dest) && !string.IsNullOrEmpty(sourceDir))
        {
            UpdateApplier.ApplySwap(dest, sourceDir, oldPid, relaunch);
        }
    }
}
