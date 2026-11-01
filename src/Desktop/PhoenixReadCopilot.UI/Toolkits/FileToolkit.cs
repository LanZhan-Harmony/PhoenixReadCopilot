using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using PhoenixReadCopilot.UI.Helpers;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;
using WinUIKernel.Share.Toolkits;

namespace PhoenixReadCopilot.UI.Toolkits;

/// <summary>
/// 文件管理工具.
/// </summary>
internal sealed class FileToolkit : IFileToolkit
{
    /// <summary>
    /// 本地数据是否存在.
    /// </summary>
    /// <returns>结果.</returns>
    public static bool IsLocalDataExist(string fileName, string folderName = "")
    {
        var path = string.IsNullOrEmpty(folderName)
            ? Path.Combine(PathHelper.RootFolderPath, fileName)
            : Path.Combine(PathHelper.RootFolderPath, folderName, fileName);
        return File.Exists(path);
    }

    /// <inheritdoc/>
    public async Task<StorageFile> PickFileAsync(string extension, object windowInstance)
    {
        var picker = new FileOpenPicker();
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(windowInstance));
        foreach (var ext in extension.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            picker.FileTypeFilter.Add(ext);
        }

        picker.SuggestedStartLocation = PickerLocationId.Desktop;
        return await picker.PickSingleFileAsync().AsTask();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<StorageFile>?> PickMultipleFileAsync(
        string extension,
        object windowInstance
    )
    {
        try
        {
            var picker = new FileOpenPicker();
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(windowInstance));
            foreach (var ext in extension.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                picker.FileTypeFilter.Add(ext);
            }

            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            return await picker.PickMultipleFilesAsync().AsTask();
        }
        catch (Exception)
        {
            return default;
        }
    }

    /// <inheritdoc/>
    public async Task<StorageFile?> SaveFileAsync(string extension, object windowInstance)
    {
        try
        {
            var picker = new FileSavePicker();
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(windowInstance));
            var exts = extension.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var ext in exts)
            {
                picker.FileTypeChoices.Add(ext, [ext]);
            }

            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            return await picker.PickSaveFileAsync().AsTask();
        }
        catch (Exception)
        {
            return default;
        }
    }

    /// <inheritdoc/>
    public async Task<StorageFolder> PickFolderAsync(object windowInstance)
    {
        var picker = new FolderPicker();
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(windowInstance));
        picker.SuggestedStartLocation = PickerLocationId.Desktop;
        picker.FileTypeFilter.Add("*");
        return await picker.PickSingleFolderAsync().AsTask();
    }

    /// <inheritdoc/>
    public async Task<T> ReadLocalDataAsync<T>(
        string fileName,
        JsonTypeInfo<T> typeInfo,
        string defaultValue = "{}",
        string folderName = ""
    )
    {
        var targetFolder = string.IsNullOrEmpty(folderName)
            ? PathHelper.RootFolderPath
            : Path.Combine(PathHelper.RootFolderPath, folderName);
        var filePath = Path.Combine(targetFolder, fileName);
        var content = defaultValue;
        try
        {
            if (File.Exists(filePath))
            {
                var fileContent = await File.ReadAllTextAsync(filePath);
                if (!string.IsNullOrEmpty(fileContent))
                {
                    content = fileContent;
                }
            }
        }
        catch (FileNotFoundException) { }

        return (
            typeof(T) == typeof(string)
                ? (T)content.Clone()
                : JsonSerializer.Deserialize<T>(content, typeInfo)
        )!;
    }

    /// <inheritdoc/>
    public async Task WriteLocalDataAsync<T>(
        string fileName,
        T data,
        JsonTypeInfo<T> typeInfo,
        string folderName = ""
    )
    {
        var targetFolder = string.IsNullOrEmpty(folderName)
            ? PathHelper.RootFolderPath
            : Path.Combine(PathHelper.RootFolderPath, folderName);
        Directory.CreateDirectory(targetFolder);

        var filePath = Path.Combine(targetFolder, fileName);
        var writeContent =
            data is string ? data.ToString() : JsonSerializer.Serialize(data, typeInfo);
        var tmpPath = filePath + ".tmp";
        await File.WriteAllTextAsync(tmpPath, writeContent);
        File.Move(tmpPath, filePath, true);
    }

    /// <inheritdoc/>
    public Task DeleteLocalDataAsync(string fileName, string folderName = "")
    {
        var targetFolder = string.IsNullOrEmpty(folderName)
            ? PathHelper.RootFolderPath
            : Path.Combine(PathHelper.RootFolderPath, folderName);
        var filePath = Path.Combine(targetFolder, fileName);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        return Task.CompletedTask;
    }
}
