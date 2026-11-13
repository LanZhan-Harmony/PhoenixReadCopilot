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
    /// 认证文件夹路径.
    /// </summary>
    string AuthFolderPath { get; }

    /// <summary>
    /// 数据文件夹路径
    /// </summary>
    string DataFolderPath { get; }

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
}
