using Microsoft.Windows.Storage;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Helpers;

namespace PhoenixReadCopilot.Services;

internal class PathService : IPathService
{
    public string RootFolderPath { get; }

    public string SettingsFolderPath { get; }

    public string AuthFolderPath { get; }

    public string DataFolderPath { get; }

    public string LogFolderPath { get; }

    public string TempFolderPath { get; }

    public string CacheFolderPath { get; }

    public PathService()
    {
        RootFolderPath = GetApplicationDataRoot();
        SettingsFolderPath = Path.Combine(RootFolderPath, "Settings");
        AuthFolderPath = Path.Combine(RootFolderPath, "Auth");
        DataFolderPath = Path.Combine(RootFolderPath, "Data");
        LogFolderPath = Path.Combine(RootFolderPath, "Logs");
        TempFolderPath = Path.Combine(RootFolderPath, "Temp");
        CacheFolderPath = Path.Combine(RootFolderPath, "Cache");
        EnsureDirectoryExists();
    }

    private void EnsureDirectoryExists()
    {
        Directory.CreateDirectory(RootFolderPath);
        Directory.CreateDirectory(SettingsFolderPath);
        Directory.CreateDirectory(AuthFolderPath);
        Directory.CreateDirectory(DataFolderPath);
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
