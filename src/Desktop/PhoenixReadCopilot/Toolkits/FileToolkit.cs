using Microsoft.UI;
using Microsoft.Windows.Storage.Pickers;
using PhoenixReadCopilot.Helpers;
using WinUIKernel.Share.Toolkits;

namespace PhoenixReadCopilot.Toolkits;

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
        string path = string.IsNullOrEmpty(folderName)
            ? Path.Combine(PathHelper.RootFolderPath, fileName)
            : Path.Combine(PathHelper.RootFolderPath, folderName, fileName);
        return File.Exists(path);
    }

    /// <inheritdoc/>
    public async Task<string?> PickFileAsync(string extension, WindowId windowId)
    {
        try
        {
            FileOpenPicker picker = new(windowId);
            foreach (string ext in extension.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                picker.FileTypeFilter.Add(ext);
            }

            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            PickFileResult? result = await picker.PickSingleFileAsync();
            return result?.Path;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> PickMultipleFileAsync(string extension, WindowId windowId)
    {
        try
        {
            FileOpenPicker picker = new(windowId);
            foreach (string ext in extension.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                picker.FileTypeFilter.Add(ext);
            }

            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            IReadOnlyList<PickFileResult> results = await picker.PickMultipleFilesAsync();
            return [.. results.Select(r => r.Path)];
        }
        catch (Exception)
        {
            return [];
        }
    }

    /// <inheritdoc/>
    public async Task<string?> SaveFileAsync(string extension, WindowId windowId)
    {
        try
        {
            FileSavePicker picker = new(windowId);
            string[] exts = extension.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (string ext in exts)
            {
                picker.FileTypeChoices.Add(ext, [ext]);
            }

            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            PickFileResult? result = await picker.PickSaveFileAsync();
            return result?.Path;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<string?> PickFolderAsync(WindowId windowId)
    {
        try
        {
            FolderPicker picker = new(windowId)
            {
                SuggestedStartLocation = PickerLocationId.Desktop,
            };

            PickFolderResult? result = await picker.PickSingleFolderAsync();
            return result?.Path;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<T> ReadLocalDataAsync<T>(
        string fileName,
        string defaultValue = "{}",
        string folderName = ""
    )
    {
        string targetFolder = string.IsNullOrEmpty(folderName)
            ? PathHelper.RootFolderPath
            : Path.Combine(PathHelper.RootFolderPath, folderName);
        string filePath = Path.Combine(targetFolder, fileName);
        string content = defaultValue;
        try
        {
            if (File.Exists(filePath))
            {
                string fileContent = await File.ReadAllTextAsync(filePath);
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
                : JsonAotSerializer.Deserialize<T>(content)
        )!;
    }

    /// <inheritdoc/>
    public async Task WriteLocalDataAsync<T>(string fileName, T data, string folderName = "")
    {
        string targetFolder = string.IsNullOrEmpty(folderName)
            ? PathHelper.RootFolderPath
            : Path.Combine(PathHelper.RootFolderPath, folderName);
        Directory.CreateDirectory(targetFolder);

        string filePath = Path.Combine(targetFolder, fileName);
        string? writeContent = data is string ? data.ToString() : JsonAotSerializer.Serialize(data);
        string tmpPath = filePath + ".tmp";
        await File.WriteAllTextAsync(tmpPath, writeContent);
        File.Move(tmpPath, filePath, true);
    }

    /// <inheritdoc/>
    public Task DeleteLocalDataAsync(string fileName, string folderName = "")
    {
        string targetFolder = string.IsNullOrEmpty(folderName)
            ? PathHelper.RootFolderPath
            : Path.Combine(PathHelper.RootFolderPath, folderName);
        string filePath = Path.Combine(targetFolder, fileName);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        return Task.CompletedTask;
    }
}
