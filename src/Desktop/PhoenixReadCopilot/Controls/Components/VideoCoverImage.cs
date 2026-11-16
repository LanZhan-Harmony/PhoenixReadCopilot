using Microsoft.UI.Composition;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using PhoenixReadCopilot.Helpers.Animations;

namespace PhoenixReadCopilot.Controls.Components;

/// <summary>
/// 视频封面图片控件.
/// </summary>
public sealed partial class VideoCoverImage : Control
{
    private Image? _image;

    #region 依赖属性定义
    /// <summary>
    /// <see cref="Source"/> 的依赖属性.
    /// </summary>
    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
        nameof(Source),
        typeof(Uri),
        typeof(VideoCoverImage),
        new PropertyMetadata(null, OnSourcePropertyChanged)
    );

    /// <summary>
    /// <see cref="DecodePixelWidth"/> 的依赖属性.
    /// </summary>
    public static readonly DependencyProperty DecodePixelWidthProperty =
        DependencyProperty.Register(
            nameof(DecodePixelWidth),
            typeof(int),
            typeof(VideoCoverImage),
            new PropertyMetadata(300, OnDecodePixelWidthPropertyChanged)
        );

    /// <summary>
    /// <see cref="Stretch"/> 的依赖属性.
    /// </summary>
    public static readonly DependencyProperty StretchProperty = DependencyProperty.Register(
        nameof(Stretch),
        typeof(Stretch),
        typeof(VideoCoverImage),
        new PropertyMetadata(Stretch.UniformToFill)
    );
    #endregion

    #region 属性包装器
    /// <summary>
    /// 封面图片地址.
    /// </summary>
    public Uri? Source
    {
        get => (Uri?)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    /// <summary>
    /// 解码目标像素宽度，默认为 300.
    /// </summary>
    public int DecodePixelWidth
    {
        get => (int)GetValue(DecodePixelWidthProperty);
        set => SetValue(DecodePixelWidthProperty, value);
    }

    /// <summary>
    /// 封面拉伸方式 (默认为 UniformToFill).
    /// </summary>
    public Stretch Stretch
    {
        get => (Stretch)GetValue(StretchProperty);
        set => SetValue(StretchProperty, value);
    }
    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="VideoCoverImage"/> class.
    /// </summary>
    public VideoCoverImage()
    {
        DefaultStyleKey = typeof(VideoCoverImage);
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (_image is not null)
        {
            _image.ImageOpened -= OnImageOpened;
            _image.ImageFailed -= OnImageFailed;
        }

        _image = GetTemplateChild("CoverImage") as Image;

        if (_image is not null)
        {
            _image.ImageOpened += OnImageOpened;
            _image.ImageFailed += OnImageFailed;
        }

        QueueLoadImage();
    }

    private static void OnSourcePropertyChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (d is VideoCoverImage control)
        {
            control.QueueLoadImage();
        }
    }

    private static void OnDecodePixelWidthPropertyChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (d is VideoCoverImage control)
        {
            control.QueueLoadImage();
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        QueueLoadImage();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_image is not null)
        {
            _image.Tag = null;
            _image.Source = null;
            Visual? visual = _image.GetElementVisual();
            if (visual is not null)
            {
                visual.StopAnimation(nameof(visual.Opacity));
                visual.Opacity = 0f;
            }
        }
    }

    private void QueueLoadImage()
    {
        if (_image is null)
        {
            return;
        }

        Visual? visual = _image.GetElementVisual();

        if (Source is null)
        {
            _image.Tag = null;
            if (visual is not null)
            {
                visual.StopAnimation(nameof(visual.Opacity));
                visual.Opacity = 0f;
            }
            if (_image.Source is BitmapImage currentBmp)
            {
                currentBmp.UriSource = null;
            }
            return;
        }

        if (
            _image.Source is BitmapImage loadedBmp
            && loadedBmp.UriSource == Source
            && visual is not null
            && visual.Opacity >= 1f
        )
        {
            _image.Tag = Source;
            return;
        }

        if (Equals(_image.Tag, Source))
        {
            return;
        }

        _image.Tag = Source;
        if (visual is not null)
        {
            visual.StopAnimation(nameof(visual.Opacity));
            visual.Opacity = 0f;
        }

        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            if (IsLoaded && Equals(_image.Tag, Source))
            {
                if (_image.Source is not BitmapImage bitmap)
                {
                    bitmap = new BitmapImage
                    {
                        DecodePixelType = DecodePixelType.Logical,
                        DecodePixelWidth = DecodePixelWidth > 0 ? DecodePixelWidth : 300,
                        CreateOptions = BitmapCreateOptions.IgnoreImageCache,
                    };
                    _image.Source = bitmap;
                }

                if (bitmap.UriSource != Source)
                {
                    bitmap.UriSource = Source;
                }
            }
        });
    }

    private void OnImageOpened(object sender, RoutedEventArgs e)
    {
        if (_image is null)
        {
            return;
        }

        Visual visual = _image.GetElementVisual()!;
        CompositionAnimation fadeAnimation = CompositionFactory.CreateFade(
            visual.Compositor,
            1f,
            0f,
            180
        );
        visual.StartAnimation(nameof(visual.Opacity), fadeAnimation);
    }

    private void OnImageFailed(object sender, ExceptionRoutedEventArgs e)
    {
        if (_image is null)
        {
            return;
        }

        Visual? visual = _image.GetElementVisual();
        if (visual is not null)
        {
            visual.StopAnimation(nameof(visual.Opacity));
            visual.Opacity = 0f;
        }
    }

    /// <summary>
    /// 当容器被 ItemsRepeater 回收移出视口时清理图片，避免离屏容器常驻显存.
    /// </summary>
    public void Clear()
    {
        if (_image is null)
        {
            return;
        }

        _image.Tag = null;
        Visual? visual = _image.GetElementVisual();
        if (visual is not null)
        {
            visual.StopAnimation(nameof(visual.Opacity));
            visual.Opacity = 0f;
        }

        if (!IsLoaded)
        {
            _image.Source = null;
        }
        else if (_image.Source is BitmapImage bmp)
        {
            bmp.UriSource = null;
        }
    }

    /// <summary>
    /// 静态清理辅助方法，供 ItemsRepeater.ElementClearing 快捷调用.
    /// </summary>
    public static void Clear(FrameworkElement container)
    {
        if (container is VideoCoverImage cover)
        {
            cover.Clear();
        }
        else if (container.FindName("CoverImage") is VideoCoverImage innerCover)
        {
            innerCover.Clear();
        }
    }
}
