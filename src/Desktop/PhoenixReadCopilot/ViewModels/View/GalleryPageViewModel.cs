using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Windows.Storage.Pickers;
using PhoenixReadCopilot.Contracts.Services;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Models.Appearance;
using PhoenixReadCopilot.Strings;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.UserProfile;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 图片库页面视图模型.
/// </summary>
internal sealed partial class GalleryPageViewModel : ViewModelBase
{
    private readonly IWindowService _windowService;
    private readonly INotificationService _notificationService;
    private readonly IPathService _pathService = Ioc.Default.GetRequiredService<IPathService>();

    [ObservableProperty]
    public partial List<AppImage>? Images { get; set; }

    [ObservableProperty]
    public partial bool IsGroup { get; set; }

    [ObservableProperty]
    public partial bool IsMenuHide { get; set; }

    [ObservableProperty]
    public partial AppImage SelectedImage { get; set; }

    public GalleryPageViewModel(
        AppImage image,
        List<AppImage>? images,
        IWindowService windowService,
        INotificationService notificationService
    )
    {
        SelectedImage = image;
        Images = images ?? [image];
        IsGroup = Images?.Count > 1;
        IsMenuHide = false;
        _windowService = windowService;
        _notificationService = notificationService;
    }

    public void ChangeImage(AppImage image)
    {
        if (image is null || SelectedImage == image)
        {
            return;
        }

        SelectedImage = image;
    }

    [RelayCommand]
    private void NextImage()
    {
        int index = Images.ToList().IndexOf(SelectedImage);
        if (index == Images.Count - 1)
        {
            return;
        }

        index++;
        ChangeImage(Images.ElementAt(index));
    }

    [RelayCommand]
    private void PreviousImage()
    {
        int index = Images.ToList().IndexOf(SelectedImage);
        if (index == 0)
        {
            return;
        }

        index--;
        ChangeImage(Images.ElementAt(index));
    }

    [RelayCommand]
    private void CopyImage()
    {
        if (SelectedImage is null)
        {
            return;
        }

        DataPackage dp = new();
        dp.SetWebLink(SelectedImage.Uri);
        dp.SetBitmap(RandomAccessStreamReference.CreateFromUri(SelectedImage.Uri));
        Clipboard.SetContent(dp);
        _notificationService.ShowInAppNotification(Resources.Copied, NotificationType.Success);
    }

    [RelayCommand]
    private async Task SaveImageAsync()
    {
        if (SelectedImage is null)
        {
            return;
        }

        string fileName = Path.GetFileName(SelectedImage.Uri.AbsolutePath);
        string extension = Path.GetExtension(fileName);
        FileSavePicker savePicker = new(_windowService.GetActiveWindowId() ?? default)
        {
            SuggestedStartLocation = PickerLocationId.PicturesLibrary,
            SuggestedFileName = fileName,
        };

        savePicker.FileTypeChoices.Add($"{extension.TrimStart('.').ToUpper()} 图片", [extension]);
        PickFileResult? file = await savePicker.PickSaveFileAsync();
        if (file is null)
        {
            return;
        }

        using HttpClient client = new();
        using HttpResponseMessage response = await client.GetAsync(
            SelectedImage.Uri,
            HttpCompletionOption.ResponseHeadersRead
        );
        await using FileStream fileStream = new(
            file.Path,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            81920,
            FileOptions.Asynchronous
        );
        await response.Content.CopyToAsync(fileStream);
        _notificationService.ShowInAppNotification(Resources.Saved, NotificationType.Success);
    }

    public async Task SetWallPaperOrLockScreenAsync(bool isWallPaper)
    {
        if (SelectedImage is null)
        {
            return;
        }

        UserProfilePersonalizationSettings profileSettings =
            UserProfilePersonalizationSettings.Current;
        string fileName = Path.GetFileName(SelectedImage.Uri.AbsolutePath);
        string tempFolder = Path.Combine(_pathService.TempFolderPath, "TempImages");
        Directory.CreateDirectory(tempFolder);
        string filePath = Path.Combine(tempFolder, fileName);
        using HttpClient client = new();
        using HttpResponseMessage response = await client.GetAsync(
            SelectedImage.Uri,
            HttpCompletionOption.ResponseHeadersRead
        );
        await using FileStream fileStream = new(
            filePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            81920,
            FileOptions.Asynchronous
        );
        await response.Content.CopyToAsync(fileStream);

        StorageFile file = await StorageFile.GetFileFromPathAsync(filePath);
        bool result = isWallPaper
            ? await profileSettings.TrySetWallpaperImageAsync(file)
            : await profileSettings.TrySetLockScreenImageAsync(file);
        if (result)
        {
            _notificationService.ShowInAppNotification(Resources.SetSuccess, NotificationType.Success);
        }
        else
        {
            _notificationService.ShowInAppNotification(Resources.SetFailed, NotificationType.Error);
        }
    }
}
