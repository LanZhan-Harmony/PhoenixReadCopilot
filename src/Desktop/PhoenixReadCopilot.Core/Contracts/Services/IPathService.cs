namespace PhoenixReadCopilot.Core.Contracts.Services;

public interface IPathService
{
    /// <summary>
    /// 应用数据根目录.
    /// </summary>
    string RootFolderPath { get; }

    /// <summary>
    /// 设置文件夹路径.
    /// </summary>
    string SettingsFolderPath { get; }

    /// <summary>
    /// 日志文件夹路径.
    /// </summary>
    string LogFolderPath { get; }

    /// <summary>
    /// 临时文件夹路径.
    /// </summary>
    string TempFolderPath { get; }

    /// <summary>
    /// 缓存文件夹路径.
    /// </summary>
    string CacheFolderPath { get; }

    /// <summary>
    /// 认证文件夹路径.
    /// </summary>
    string AuthFolderPath { get; }

    /// <summary>
    /// 收藏文件夹路径.
    /// </summary>
    string FavoriteFolderPath { get; }

    /// <summary>
    /// 关注文件夹路径.
    /// </summary>
    string FollowFolderPath { get; }

    /// <summary>
    /// 历史文件夹路径.
    /// </summary>
    string HistoryFolderPath { get; }

    /// <summary>
    /// 点赞文件夹路径.
    /// </summary>
    string LikeFolderPath { get; }

    /// <summary>
    /// 固定文件夹路径.
    /// </summary>
    string PinFolderPath { get; }

    /// <summary>
    /// 媒体URL文件夹路径.
    /// </summary>
    string MediaFolderPath { get; }

    /// <summary>
    /// 预约文件夹路径.
    /// </summary>
    string ReservationFolderPath { get; }

    /// <summary>
    /// 搜索文件夹路径.
    /// </summary>
    string SearchFolderPath { get; }

    /// <summary>
    /// 稍后再看文件夹路径.
    /// </summary>
    string ViewLaterFolderPath { get; }
}
