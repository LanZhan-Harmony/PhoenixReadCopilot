using Microsoft.UI;

namespace WinUIKernel.Share.Toolkits;

/// <summary>
/// File toolkit.
/// </summary>
public interface IFileToolkit
{
    /// <summary>
    /// 选择文件.
    /// </summary>
    /// <param name="extension">扩展名.</param>
    /// <param name="windowId"></param>
    /// <returns>文件对象（可能为空）.</returns>
    Task<string?> PickFileAsync(string extension, WindowId windowId);

    /// <summary>
    /// 选择多个文件.
    /// </summary>
    /// <param name="extension">扩展名.</param>
    /// <param name="windowId"></param>
    /// <returns>文件对象（可能为空）.</returns>
    Task<IReadOnlyList<string>> PickMultipleFileAsync(string extension, WindowId windowId);

    /// <summary>
    /// 选择文件夹.
    /// </summary>
    /// <param name="windowId"></param>
    /// <returns>文件夹.</returns>
    Task<string?> PickFolderAsync(WindowId windowId);

    /// <summary>
    /// 保存文件.
    /// </summary>
    /// <param name="extension">扩展名.</param>
    /// <param name="windowId"></param>
    /// <returns>文件.</returns>
    Task<string?> SaveFileAsync(string extension, WindowId windowId);

    /// <summary>
    /// Get local data and convert.
    /// </summary>
    /// <typeparam name="T">Conversion target type.</typeparam>
    /// <param name="fileName">File name.</param>
    /// <param name="typeInfo">Json type info for deserialize.</param>
    /// <param name="defaultValue">The default value when the file does not exist or has no content.</param>
    /// <param name="folderName">The folder to which the file belongs.</param>
    /// <returns>Converted result.</returns>
    Task<T> ReadLocalDataAsync<T>(
        string fileName,
        string defaultValue = "{}",
        string folderName = ""
    );

    /// <summary>
    /// Write data to local file.
    /// </summary>
    /// <typeparam name="T">Type of data.</typeparam>
    /// <param name="fileName">File name.</param>
    /// <param name="data">Data to be written.</param>
    /// <param name="typeInfo">Type info for serialize.</param>
    /// <param name="folderName">The folder to which the file belongs.</param>
    /// <returns><see cref="Task"/>.</returns>
    Task WriteLocalDataAsync<T>(string fileName, T data, string folderName = "");

    /// <summary>
    /// Delete local data file.
    /// </summary>
    /// <param name="fileName">File name.</param>
    /// <param name="folderName">The folder to which the file belongs.</param>
    /// <returns><see cref="Task"/>.</returns>
    Task DeleteLocalDataAsync(string fileName, string folderName = "");
}
