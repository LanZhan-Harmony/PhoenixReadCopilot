using System.Buffers;
using System.Runtime.InteropServices.WindowsRuntime;
using CommunityToolkit.WinUI.Animations;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using PhoenixReadCopilot.ViewModels.Items;
using Windows.Storage.Streams;

namespace PhoenixReadCopilot.Controls.Home;

internal partial class HeaderTile : Button
{
    private const int SampleSize = 16;
    private const int LightLuminanceThreshold = 140;

    private Image? _image;
    private bool _isImageLoaded;

    /// <summary>
    /// 关联的短剧视图模型.
    /// </summary>
    public DramaItemViewModel? ViewModel
    {
        get => (DramaItemViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(DramaItemViewModel),
        typeof(HeaderTile),
        new PropertyMetadata(null)
    );

    public string ImageUrl
    {
        get => (string)GetValue(ImageUrlProperty);
        set => SetValue(ImageUrlProperty, value);
    }

    public static readonly DependencyProperty ImageUrlProperty = DependencyProperty.Register(
        nameof(ImageUrl),
        typeof(string),
        typeof(HeaderTile),
        new PropertyMetadata(null, (d, _) => ((HeaderTile)d).OnImageUrlChanged())
    );

    public string Header
    {
        get => (string)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
        nameof(Header),
        typeof(string),
        typeof(HeaderTile),
        new PropertyMetadata(null)
    );

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
        nameof(Description),
        typeof(string),
        typeof(HeaderTile),
        new PropertyMetadata(defaultValue: null)
    );

    public string SampleID
    {
        get => (string)GetValue(SampleIDProperty);
        set => SetValue(SampleIDProperty, value);
    }

    public static readonly DependencyProperty SampleIDProperty = DependencyProperty.Register(
        nameof(SampleID),
        typeof(string),
        typeof(HeaderTile),
        new PropertyMetadata(string.Empty)
    );

    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(
        nameof(IsSelected),
        typeof(bool),
        typeof(HeaderTile),
        new PropertyMetadata(defaultValue: false, (d, e) =>
            ((HeaderTile)d).IsSelectedChanged((bool)e.OldValue, (bool)e.NewValue)
        )
    );

    /// <summary>
    /// 封面图片是否已完成加载.
    /// </summary>
    public bool IsImageLoaded => _isImageLoaded;

    /// <summary>
    /// 封面图片加载完成事件.
    /// </summary>
    public event EventHandler? ImageLoaded;

    public HeaderTile()
    {
        DefaultStyleKey = typeof(HeaderTile);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (_image is not null)
        {
            _image.ImageOpened -= OnImageOpened;
            _image.ImageFailed -= OnImageFailed;
        }

        _image = GetTemplateChild("PART_Image") as Image;
        if (_image is not null)
        {
            _image.ImageOpened += OnImageOpened;
            _image.ImageFailed += OnImageFailed;
            UpdateImageSource();
        }
    }

    /// <summary>
    /// 极速采样当前封面缩略图（16x16）判断主色调是否为浅色.
    /// </summary>
    public async Task<bool?> IsLightImageAsync()
    {
        if (
            !_isImageLoaded
            || _image is null
            || _image.ActualWidth <= 0
            || _image.ActualHeight <= 0
        )
        {
            return null;
        }

        try
        {
            RenderTargetBitmap rtb = new();
            await rtb.RenderAsync(_image, SampleSize, SampleSize);
            if (rtb.PixelWidth <= 0 || rtb.PixelHeight <= 0)
            {
                return null;
            }

            IBuffer pixelBuffer = await rtb.GetPixelsAsync();
            return ComputeIsLightFromBuffer(pixelBuffer);
        }
        catch
        {
            return null;
        }
    }

    private static bool? ComputeIsLightFromBuffer(IBuffer pixelBuffer)
    {
        int byteLength = (int)pixelBuffer.Length;
        if (byteLength < 4)
        {
            return null;
        }

        byte[]? rented = byteLength > 1024 ? ArrayPool<byte>.Shared.Rent(byteLength) : null;
        try
        {
            Span<byte> pixels = rented is not null
                ? rented.AsSpan(0, byteLength)
                : stackalloc byte[byteLength];

            using Stream stream = pixelBuffer.AsStream();
            int read = 0;
            while (read < byteLength)
            {
                int n = stream.Read(pixels[read..]);
                if (n == 0)
                {
                    break;
                }

                read += n;
            }

            long totalLuminance = 0;
            long validPixels = 0;
            for (int i = 0; i + 3 < read; i += 4)
            {
                byte b = pixels[i];
                byte g = pixels[i + 1];
                byte r = pixels[i + 2];
                byte a = pixels[i + 3];
                if (a < 16)
                {
                    continue;
                }

                totalLuminance += (77 * r + 150 * g + 29 * b) >> 8;
                validPixels++;
            }

            if (validPixels == 0)
            {
                return null;
            }

            return (totalLuminance / validPixels) >= LightLuminanceThreshold;
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }

    private void OnImageUrlChanged()
    {
        UpdateImageSource();
    }

    private void UpdateImageSource()
    {
        _isImageLoaded = false;
        if (_image is null)
        {
            return;
        }

        if (Uri.TryCreate(ImageUrl, UriKind.Absolute, out Uri? uri))
        {
            _image.Source = new BitmapImage(uri)
            {
                DecodePixelType = DecodePixelType.Logical,
                DecodePixelWidth = 250,
            };
        }
        else
        {
            _image.Source = null;
        }
    }

    private void OnImageOpened(object sender, RoutedEventArgs e)
    {
        _isImageLoaded = true;
        ImageLoaded?.Invoke(this, EventArgs.Empty);
    }

    private void OnImageFailed(object sender, ExceptionRoutedEventArgs e)
    {
        _isImageLoaded = false;
    }

    protected virtual void IsSelectedChanged(object oldValue, object newValue)
    {
        OnIsSelectedChanged();
    }

    private void OnIsSelectedChanged()
    {
        if (IsSelected)
        {
            Canvas.SetZIndex(this, 10);
            VisualStateManager.GoToState(this, "Selected", true);
            AnimationSet selectAnimation =
            [
                new ScaleAnimation() { To = "1.0", Duration = TimeSpan.FromMilliseconds(600) },
                new OpacityDropShadowAnimation() { To = 0.4 },
                new BlurRadiusDropShadowAnimation() { To = 24 },
            ];
            selectAnimation.Start(this);
        }
        else
        {
            VisualStateManager.GoToState(this, "NotSelected", true);
            AnimationSet deselectAnimation =
            [
                new ScaleAnimation() { To = "0.8", Duration = TimeSpan.FromMilliseconds(350) },
                new OpacityDropShadowAnimation() { To = 0.2 },
                new BlurRadiusDropShadowAnimation() { To = 12 },
            ];
            deselectAnimation.Completed += (s, e) =>
            {
                Canvas.SetZIndex(this, 0);
            };
            deselectAnimation.Start(this);
        }
    }
}
