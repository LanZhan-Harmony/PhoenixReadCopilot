using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using PhoenixReadCopilot.Core.Models.Appearance;
using PhoenixReadCopilot.ViewModels.Core;
using PhoenixReadCopilot.ViewModels.View;

namespace PhoenixReadCopilot.Pages;

/// <summary>
/// 图片库页面.
/// </summary>
internal sealed partial class GalleryPage : GalleryPageBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GalleryPage"/> class.
    /// </summary>
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
                Ioc.Default.GetRequiredService<AppViewModel>()
            );
        }
    }

    /// <inheritdoc/>
    protected override void OnPageLoaded()
    {
        FactoryBlock.Text = 1.ToString("p00");
        ImageScrollView.ZoomTo(0.8f, null);
        ImageView.ScrollView.VerticalScrollMode = ScrollingScrollMode.Disabled;
        ImageView.ScrollView.HorizontalScrollMode = ScrollingScrollMode.Auto;
        ImageView.ScrollView.HorizontalScrollBarVisibility = ScrollingScrollBarVisibility.Auto;
        int index = ViewModel.Images.ToList().IndexOf(ViewModel.SelectedImage);
        ImageView.Select(index);
    }

    protected override void OnPageUnloaded() => ImageView.ItemsSource = null;

    private void OnScrollViewTapped(object sender, TappedRoutedEventArgs e) =>
        ViewModel.IsMenuHide = !ViewModel.IsMenuHide;

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
        _ = ViewModel.SetWallPaperOrLockScreenAsync(true);

    private void OnSettingToLockScreenClickAsync(object sender, RoutedEventArgs e) =>
        _ = ViewModel.SetWallPaperOrLockScreenAsync(false);

    private void OnImageSelectionChanged(ItemsView sender, ItemsViewSelectionChangedEventArgs args)
    {
        AppImage? image = sender.SelectedItem as AppImage;
        ViewModel.ChangeImage(image);
    }
}

/// <summary>
/// 图片库页面基类.
/// </summary>
internal abstract class GalleryPageBase : LayoutPageBase<GalleryPageViewModel> { }
