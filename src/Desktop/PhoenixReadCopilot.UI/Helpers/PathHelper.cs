using Windows.Storage;

namespace PhoenixReadCopilot.UI.Helpers;

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
        LogFolderPath = Path.Combine(RootFolderPath, "Logs");
        TempFolderPath = Path.Combine(RootFolderPath, "Temp");
        CacheFolderPath = Path.Combine(RootFolderPath, "Cache");
        EnsureDirectoryExists();
    }

    private static void EnsureDirectoryExists()
    {
        Directory.CreateDirectory(RootFolderPath);
        Directory.CreateDirectory(SettingsFolderPath);
        Directory.CreateDirectory(LogFolderPath);
        Directory.CreateDirectory(TempFolderPath);
        Directory.CreateDirectory(CacheFolderPath);
    }

    private static string GetApplicationDataRoot()
    {
        if (RuntimeHelper.IsMSIX)
        {
            return ApplicationData.Current.LocalFolder.Path;
        }

        var portablePath = Path.Combine(AppContext.BaseDirectory, "AppData");
        try
        {
            Directory.CreateDirectory(portablePath);
            using var fs = File.Create(
                Path.Combine(portablePath, Path.GetRandomFileName()),
                1,
                FileOptions.DeleteOnClose
            );
            return portablePath;
        }
        catch
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PhoenixReadCopilot"
            );
            Directory.CreateDirectory(appDataPath);
            return appDataPath;
        }
    }
}
