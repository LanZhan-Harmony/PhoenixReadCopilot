using FluentIcons.Common;
using Microsoft.UI.Composition;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using PhoenixReadCopilot.Helpers.Animations;
using Symbol = FluentIcons.Common.Symbol;

namespace PhoenixReadCopilot.Controls.Components;

/// <summary>
/// 视频封面图片控件.
/// </summary>
[TemplatePart(Name = ImagePartName, Type = typeof(Image))]
internal sealed partial class CoverImage : Control
{
    private const string ImagePartName = "PART_Image";

    private Image? _image;

    /// <summary>
    /// 封面图片地址.
    /// </summary>
    public Uri? Source
    {
        get => (Uri?)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
        nameof(Source),
        typeof(Uri),
        typeof(CoverImage),
        new PropertyMetadata(null, OnSourcePropertyChanged)
    );

    /// <summary>
    /// 图标.
    /// </summary>
    public Symbol Symbol
    {
        get => (Symbol)GetValue(SymbolProperty);
        set => SetValue(SymbolProperty, value);
    }

    public static readonly DependencyProperty SymbolProperty = DependencyProperty.Register(
        nameof(Symbol),
        typeof(Symbol),
        typeof(CoverImage),
        new PropertyMetadata(Symbol.VideoClipMultiple)
    );

    /// <summary>
    /// 图标填充类型
    /// </summary>
    public IconVariant IconVarient
    {
        get => (IconVariant)GetValue(IconVarientProperty);
        set => SetValue(IconVarientProperty, value);
    }

    public static readonly DependencyProperty IconVarientProperty = DependencyProperty.Register(
        nameof(IconVarient),
        typeof(IconVariant),
        typeof(CoverImage),
        new PropertyMetadata(IconVariant.Filled)
    );

    /// <summary>
    /// 图标大小
    /// </summary>
    public double IconSize
    {
        get => (double)GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    public static readonly DependencyProperty IconSizeProperty = DependencyProperty.Register(
        nameof(IconSize),
        typeof(double),
        typeof(CoverImage),
        new PropertyMetadata(48d)
    );

    /// <summary>
    /// 解码目标像素宽度，默认为 300.
    /// </summary>
    public int DecodePixelWidth
    {
        get => (int)GetValue(DecodePixelWidthProperty);
        set => SetValue(DecodePixelWidthProperty, value);
    }

    public static readonly DependencyProperty DecodePixelWidthProperty =
        DependencyProperty.Register(
            nameof(DecodePixelWidth),
            typeof(int),
            typeof(CoverImage),
            new PropertyMetadata(300, OnDecodePixelWidthPropertyChanged)
        );

    /// <summary>
    /// 封面拉伸方式 (默认为 UniformToFill).
    /// </summary>
    public Stretch Stretch
    {
        get => (Stretch)GetValue(StretchProperty);
        set => SetValue(StretchProperty, value);
    }

    public static readonly DependencyProperty StretchProperty = DependencyProperty.Register(
        nameof(Stretch),
        typeof(Stretch),
        typeof(CoverImage),
        new PropertyMetadata(Stretch.UniformToFill)
    );

    /// <summary>
    /// Initializes a new instance of the <see cref="CoverImage"/> class.
    /// </summary>
    public CoverImage()
    {
        DefaultStyleKey = typeof(CoverImage);
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

        _image = GetTemplateChild(ImagePartName) as Image;

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
        if (d is CoverImage control)
        {
            control.QueueLoadImage();
        }
    }

    private static void OnDecodePixelWidthPropertyChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (d is CoverImage control)
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
        if (container is CoverImage cover)
        {
            cover.Clear();
        }
        else if (container.FindName("CoverImage") is CoverImage innerCover)
        {
            innerCover.Clear();
        }
    }
}
