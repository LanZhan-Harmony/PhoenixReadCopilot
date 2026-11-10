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
        Images = images ?? [image];
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
        _appViewModel.ShowTipCommand.Execute((Resources.Copied, InfoType.Success));
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
        FileSavePicker savePicker = new(_appViewModel.ActivatedWindow.AppWindow.Id)
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
        _appViewModel.ShowTipCommand.Execute((Resources.Saved, InfoType.Success));
    }

    [RelayCommand]
    private async Task SetWallPaperOrLockScreenAsync(bool isWallPaper)
    {
        if (SelectedImage is null)
        {
            return;
        }

        UserProfilePersonalizationSettings profileSettings =
            UserProfilePersonalizationSettings.Current;
        string fileName = Path.GetFileName(SelectedImage.Uri.AbsolutePath);
        string tempFolder = Path.Combine(PathHelper.TempFolderPath, "TempImages");
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
            _appViewModel.ShowTipCommand.Execute((Resources.SetSuccess, InfoType.Success));
        }
        else
        {
            _appViewModel.ShowTipCommand.Execute((Resources.SetFailed, InfoType.Error));
        }
    }
}
