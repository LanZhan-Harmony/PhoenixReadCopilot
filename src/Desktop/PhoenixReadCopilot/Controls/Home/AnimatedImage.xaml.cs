using CommunityToolkit.WinUI.Animations;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace PhoenixReadCopilot.Controls.Home;

internal partial class AnimatedImage : UserControl
{
    private AnimationSet? selectAnimation;

    public Uri ImageUrl
    {
        get => (Uri)GetValue(ImageUrlProperty);
        set => SetValue(ImageUrlProperty, value);
    }

    public static readonly DependencyProperty ImageUrlProperty = DependencyProperty.Register(
        nameof(ImageUrl),
        typeof(Uri),
        typeof(AnimatedImage),
        new PropertyMetadata(defaultValue: null, (d, e) =>
            ((AnimatedImage)d).IsImageChanged((Uri)e.OldValue, (Uri)e.NewValue)
        )
    );

    public AnimatedImage()
    {
        InitializeComponent();
        Unloaded += AnimatedImage_Unloaded;
    }

    private void AnimatedImage_Unloaded(object sender, RoutedEventArgs e)
    {
        if (selectAnimation is not null)
        {
            selectAnimation.Completed -= SelectAnimation_Completed;
            selectAnimation.Stop(TopImage);
            selectAnimation = null;
        }
    }

    protected virtual void IsImageChanged(Uri oldValue, Uri newValue)
    {
        OnIsImageChanged();
    }

    private void OnIsImageChanged()
    {
        if (ImageUrl is null)
        {
            BottomImage.Source = null;
            TopImage.Source = null;
            return;
        }

        // 背景图会经过大半径高斯模糊，按小尺寸解码即可大幅降低 GPU 上传与模糊开销
        BottomImage.Source = new BitmapImage(ImageUrl)
        {
            DecodePixelType = DecodePixelType.Logical,
            DecodePixelWidth = 240,
        };
        BottomImage.Opacity = 1;

        if (selectAnimation is not null)
        {
            selectAnimation.Completed -= SelectAnimation_Completed;
            selectAnimation.Stop(TopImage);
        }

        selectAnimation =
        [
            new OpacityAnimation()
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(800),
            },
        ];
        selectAnimation.Completed += SelectAnimation_Completed;
        selectAnimation.Start(TopImage);
    }

    private void SelectAnimation_Completed(object? sender, EventArgs e)
    {
        try
        {
            TopImage.Source = BottomImage.Source;
            TopImage.Opacity = 1;
        }
        catch { }
    }
}
