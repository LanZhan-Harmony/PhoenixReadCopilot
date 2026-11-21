using Microsoft.Windows.Storage;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Helpers;

namespace PhoenixReadCopilot.Services;

internal class PathService : IPathService
{
    /// <inheritdoc/>
    public string RootFolderPath { get; }

    /// <inheritdoc/>
    public string SettingsFolderPath { get; }

    /// <inheritdoc/>
    public string LogFolderPath { get; }

    /// <inheritdoc/>
    public string TempFolderPath { get; }

    /// <inheritdoc/>
    public string CacheFolderPath { get; }

    /// <inheritdoc/>
    public string AuthFolderPath { get; }

    /// <inheritdoc/>
    public string FavoriteFolderPath { get; }

    /// <inheritdoc/>
    public string FollowFolderPath { get; }

    /// <inheritdoc/>
    public string HistoryFolderPath { get; }

    /// <inheritdoc/>
    public string LikeFolderPath { get; }

    /// <inheritdoc/>
    public string PinFolderPath { get; }

    /// <inheritdoc/>
    public string MediaFolderPath { get; }

    /// <inheritdoc/>
    public string ReservationFolderPath { get; }

    /// <inheritdoc/>
    public string SearchFolderPath { get; }

    /// <inheritdoc/>
    public string ViewLaterFolderPath { get; }

    public PathService()
    {
        RootFolderPath = GetApplicationDataRoot();
        SettingsFolderPath = Path.Combine(RootFolderPath, "Settings");
        LogFolderPath = Path.Combine(RootFolderPath, "Logs");
        TempFolderPath = Path.Combine(RootFolderPath, "Temp");
        CacheFolderPath = Path.Combine(RootFolderPath, "Cache");
        AuthFolderPath = Path.Combine(RootFolderPath, "Auth");
        FavoriteFolderPath = Path.Combine(RootFolderPath, "Favorite");
        FollowFolderPath = Path.Combine(RootFolderPath, "Follow");
        HistoryFolderPath = Path.Combine(RootFolderPath, "History");
        LikeFolderPath = Path.Combine(RootFolderPath, "Like");
        PinFolderPath = Path.Combine(RootFolderPath, "Pin");
        MediaFolderPath = Path.Combine(RootFolderPath, "Media");
        ReservationFolderPath = Path.Combine(RootFolderPath, "Reservation");
        SearchFolderPath = Path.Combine(RootFolderPath, "Search");
        ViewLaterFolderPath = Path.Combine(RootFolderPath, "ViewLater");

        EnsureDirectoryExists();
    }

    private void EnsureDirectoryExists()
    {
        Directory.CreateDirectory(RootFolderPath);
        Directory.CreateDirectory(SettingsFolderPath);
        Directory.CreateDirectory(LogFolderPath);
        Directory.CreateDirectory(TempFolderPath);
        Directory.CreateDirectory(CacheFolderPath);
        Directory.CreateDirectory(AuthFolderPath);
        Directory.CreateDirectory(FavoriteFolderPath);
        Directory.CreateDirectory(FollowFolderPath);
        Directory.CreateDirectory(HistoryFolderPath);
        Directory.CreateDirectory(LikeFolderPath);
        Directory.CreateDirectory(PinFolderPath);
        Directory.CreateDirectory(MediaFolderPath);
        Directory.CreateDirectory(ReservationFolderPath);
        Directory.CreateDirectory(SearchFolderPath);
        Directory.CreateDirectory(ViewLaterFolderPath);
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
