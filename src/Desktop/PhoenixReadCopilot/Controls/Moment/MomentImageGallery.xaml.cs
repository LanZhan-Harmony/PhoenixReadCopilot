using System.Drawing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using PhoenixReadCopilot.Core.Models.Appearance;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.Controls.Moment;

/// <summary>
/// 动态图片画廊.
/// </summary>
public sealed partial class MomentImageGallery : LayoutUserControlBase
{
    /// <summary>
    /// <see cref="ItemsSource"/> 的依赖属性.
    /// </summary>
    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        nameof(ItemsSource),
        typeof(object),
        typeof(MomentImageGallery),
        new PropertyMetadata(null)
    );

    /// <summary>
    /// Initializes a new instance of the <see cref="MomentImageGallery"/> class.
    /// </summary>
    public MomentImageGallery()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 图片源.
    /// </summary>
    public object ItemsSource
    {
        get => (object)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    protected override void OnControlUnloaded() => ImageRepeater.ItemsSource = null;

    private void OnImageTapped(object sender, TappedRoutedEventArgs e)
    {
        e.Handled = true;
        AppImage? image = (sender as Image)?.Tag as AppImage;
        List<AppImage>? itemsSource = ItemsSource as List<AppImage>;
        // new GalleryWindow(image, itemsSource).Activate();
    }
}
