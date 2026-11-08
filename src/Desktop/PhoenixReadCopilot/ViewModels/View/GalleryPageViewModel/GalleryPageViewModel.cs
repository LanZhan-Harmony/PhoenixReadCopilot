using CommunityToolkit.Mvvm.Input;
using Microsoft.Windows.Storage.Pickers;
using PhoenixReadCopilot.Helpers;
using PhoenixReadCopilot.Models;
using PhoenixReadCopilot.Strings;
using PhoenixReadCopilot.ViewModels.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.UserProfile;
using WinUIKernel.Share.Base;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 图片库页面视图模型.
/// </summary>
public sealed partial class GalleryPageViewModel : ViewModelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GalleryPageViewModel"/> class.
    /// </summary>
    public GalleryPageViewModel(AppImage image, List<AppImage>? images, AppViewModel appViewModel)
    {
        SelectedImage = image;
        Images = images ?? new List<AppImage> { image };
        IsGroup = Images?.Count > 1;
        IsMenuHide = false;
        _appViewModel = appViewModel;
    }

    [RelayCommand]
    private void ChangeImage(AppImage image)
    {
        if (image == null || SelectedImage == image)
        {
            return;
        }

        SelectedImage = image;
    }

    [RelayCommand]
    private void NextImage()
    {
        var index = Images.ToList().IndexOf(SelectedImage);
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
        var index = Images.ToList().IndexOf(SelectedImage);
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

        var dp = new DataPackage();
        dp.SetWebLink(SelectedImage.Uri);
        dp.SetBitmap(RandomAccessStreamReference.CreateFromUri(SelectedImage.Uri));
        Clipboard.SetContent(dp);
        _appViewModel.ShowTipCommand.Execute((Resources.Copied, InfoType.Success));
    }

    [RelayCommand]
    private async Task SaveImageAsync()
    {
        if (SelectedImage is null)
        {
            return;
        }

        var fileName = Path.GetFileName(SelectedImage.Uri.ToString());
        var extension = Path.GetExtension(fileName);
        var savePicker = new FileSavePicker(_appViewModel.ActivatedWindow.AppWindow.Id)
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

        using var client = new HttpClient();
        using var response = await client.GetAsync(
            SelectedImage.Uri,
            HttpCompletionOption.ResponseHeadersRead
        );
        await using var fileStream = new FileStream(
            file.Path,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            81920,
            FileOptions.Asynchronous
        );
        await response.Content.CopyToAsync(fileStream);
        _appViewModel.ShowTipCommand.Execute((Resources.Saved, InfoType.Success));
    }

    [RelayCommand]
    private async Task SetWallPaperOrLockScreenAsync(bool isWallPaper)
    {
        if (SelectedImage is null)
        {
            return;
        }

        var profileSettings = UserProfilePersonalizationSettings.Current;
        var fileName = Path.GetFileName(SelectedImage.Uri.ToString());
        var tempFolder = Path.Combine(PathHelper.TempFolderPath, "TempImages");
        Directory.CreateDirectory(tempFolder);
        var filePath = Path.Combine(tempFolder, fileName);
        using var client = new HttpClient();
        using var response = await client.GetAsync(
            SelectedImage.Uri,
            HttpCompletionOption.ResponseHeadersRead
        );
        await using var fileStream = new FileStream(
            filePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            81920,
            FileOptions.Asynchronous
        );
        await response.Content.CopyToAsync(fileStream);

        var file = await StorageFile.GetFileFromPathAsync(filePath);
        var result = isWallPaper
            ? await profileSettings.TrySetWallpaperImageAsync(file)
            : await profileSettings.TrySetLockScreenImageAsync(file);
        if (result)
        {
            _appViewModel.ShowTipCommand.Execute((Resources.SetSuccess, InfoType.Success));
        }
        else
        {
            _appViewModel.ShowTipCommand.Execute((Resources.SetFailed, InfoType.Error));
        }
    }
}
