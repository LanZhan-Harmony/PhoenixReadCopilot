using Microsoft.Windows.Storage;

namespace PhoenixReadCopilot.Helpers;

/// <summary>
/// 路径管理辅助类.
/// </summary>
internal static class PathHelper
{
    /// <summary>
    /// 应用数据根目录.
    /// </summary>
    internal static string RootFolderPath { get; }

    /// <summary>
    /// 设置文件夹路径.
    /// </summary>
    internal static string SettingsFolderPath { get; }

    /// <summary>
    /// 认证文件夹路径.
    /// </summary>
    internal static string AuthFolderPath { get; }

    /// <summary>
    /// 日志文件夹路径.
    /// </summary>
    internal static string LogFolderPath { get; }

    /// <summary>
    /// 临时文件夹路径.
    /// </summary>
    internal static string TempFolderPath { get; }

    /// <summary>
    /// 缓存文件夹路径.
    /// </summary>
    internal static string CacheFolderPath { get; }

    static PathHelper()
    {
        RootFolderPath = GetApplicationDataRoot();
        SettingsFolderPath = Path.Combine(RootFolderPath, "Settings");
        AuthFolderPath = Path.Combine(RootFolderPath, "Auth");
        LogFolderPath = Path.Combine(RootFolderPath, "Logs");
        TempFolderPath = Path.Combine(RootFolderPath, "Temp");
        CacheFolderPath = Path.Combine(RootFolderPath, "Cache");
        EnsureDirectoryExists();
    }

    private static void EnsureDirectoryExists()
    {
        Directory.CreateDirectory(RootFolderPath);
        Directory.CreateDirectory(SettingsFolderPath);
        Directory.CreateDirectory(AuthFolderPath);
        Directory.CreateDirectory(LogFolderPath);
        Directory.CreateDirectory(TempFolderPath);
        Directory.CreateDirectory(CacheFolderPath);
    }

    private static string GetApplicationDataRoot()
    {
        if (RuntimeHelper.IsMSIX)
        {
            return ApplicationData.GetDefault().LocalPath;
        }

        string portablePath = Path.Combine(AppContext.BaseDirectory, "AppData");
        try
        {
            Directory.CreateDirectory(portablePath);
            using FileStream fs = File.Create(
                Path.Combine(portablePath, Path.GetRandomFileName()),
                1,
                FileOptions.DeleteOnClose
            );
            return portablePath;
        }
        catch
        {
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PhoenixReadCopilot"
            );
            Directory.CreateDirectory(appDataPath);
            return appDataPath;
        }
    }
}
