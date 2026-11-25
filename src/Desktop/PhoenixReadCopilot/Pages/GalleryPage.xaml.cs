using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using PhoenixReadCopilot.Contracts.Services;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Models.Appearance;
using PhoenixReadCopilot.ViewModels.View;

namespace PhoenixReadCopilot.Pages;

/// <summary>
/// 图片库页面.
/// </summary>
internal sealed partial class GalleryPage : Page
{
    /// <summary>
    /// 视图模型.
    /// </summary>
    public GalleryPageViewModel? ViewModel { get; private set; }

    public GalleryPage()
    {
        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        if (e.Parameter is (AppImage image, List<AppImage> list))
        {
            ViewModel = new GalleryPageViewModel(
                image,
                list,
                Ioc.Default.GetRequiredService<IWindowService>(),
                Ioc.Default.GetRequiredService<INotificationService>()
            );
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        FactoryBlock.Text = 1.ToString("p00");
        ImageScrollView.ZoomTo(0.8f, null);
        ImageView.ScrollView.VerticalScrollMode = ScrollingScrollMode.Disabled;
        ImageView.ScrollView.HorizontalScrollMode = ScrollingScrollMode.Auto;
        ImageView.ScrollView.HorizontalScrollBarVisibility = ScrollingScrollBarVisibility.Auto;
        if (ViewModel.Images is not null && ViewModel.SelectedImage is not null)
        {
            int index = ViewModel.Images.ToList().IndexOf(ViewModel.SelectedImage);
            ImageView.Select(index);
        }
    }

    private void OnScrollViewTapped(object sender, TappedRoutedEventArgs e)
    {
        if (ViewModel is not null)
        {
            ViewModel.IsMenuHide = !ViewModel.IsMenuHide;
        }
    }

    private void CheckButtonStatus()
    {
        ZoomOutButton.IsEnabled = ImageScrollView.ZoomFactor > 0.2;
        ZoomInButton.IsEnabled = ImageScrollView.ZoomFactor < 1.5;
    }

    private void OnScrollViewChanged(ScrollView sender, object args)
    {
        FactoryBlock.Text = ImageScrollView.ZoomFactor.ToString("p00");
        CheckButtonStatus();
    }

    private void OnZoomInButtonClick(object sender, RoutedEventArgs e) =>
        ImageScrollView.ZoomTo(ImageScrollView.ZoomFactor + 0.1f, null);

    private void OnZoomOutButtonClick(object sender, RoutedEventArgs e) =>
        ImageScrollView.ZoomTo(ImageScrollView.ZoomFactor - 0.1f, null);

    private void OnSettingToBackgroundClickAsync(object sender, RoutedEventArgs e) =>
        _ = ViewModel?.SetWallPaperOrLockScreenAsync(true);

    private void OnSettingToLockScreenClickAsync(object sender, RoutedEventArgs e) =>
        _ = ViewModel?.SetWallPaperOrLockScreenAsync(false);

    private void OnImageSelectionChanged(ItemsView sender, ItemsViewSelectionChangedEventArgs args)
    {
        if (ViewModel is not null && sender.SelectedItem is AppImage image)
        {
            ViewModel.ChangeImage(image);
        }
    }
}
