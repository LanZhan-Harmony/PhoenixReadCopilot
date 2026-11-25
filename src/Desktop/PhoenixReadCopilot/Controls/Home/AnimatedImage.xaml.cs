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
        selectAnimation?.Completed -= SelectAnimation_Completed;
        selectAnimation = null;
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

        BottomImage.Source = new BitmapImage(ImageUrl);
        BottomImage.Opacity = 1;

        selectAnimation?.Completed -= SelectAnimation_Completed;

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
            TopImage.Source = new BitmapImage(ImageUrl);
            TopImage.Opacity = 1;
        }
        catch { }
    }
}
