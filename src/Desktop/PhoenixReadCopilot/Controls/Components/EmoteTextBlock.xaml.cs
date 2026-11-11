using System.Text.RegularExpressions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using PhoenixReadCopilot.Forms;
using PhoenixReadCopilot.Helpers;
using PhoenixReadCopilot.Models;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.Controls.Components;

/// <summary>
/// 表情富文本.
/// </summary>
public sealed partial class EmoteTextBlock : LayoutUserControlBase
{
    /// <summary>
    /// <see cref="MaxLines"/> 的依赖属性.
    /// </summary>
    public static readonly DependencyProperty MaxLinesProperty = DependencyProperty.Register(
        nameof(MaxLines),
        typeof(int),
        typeof(EmoteTextBlock),
        new PropertyMetadata(4)
    );

    /// <summary>
    /// <see cref="Text"/> 的依赖属性.
    /// </summary>
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text),
        typeof(EmoteText),
        typeof(EmoteTextBlock),
        new PropertyMetadata(null, new PropertyChangedCallback(OnTextChanged))
    );

    /// <summary>
    /// Initializes a new instance of the <see cref="EmoteTextBlock"/> class.
    /// </summary>
    public EmoteTextBlock() => InitializeComponent();

    /// <summary>
    /// 最大行数.
    /// </summary>
    public int MaxLines
    {
        get => (int)GetValue(MaxLinesProperty);
        set => SetValue(MaxLinesProperty, value);
    }

    /// <summary>
    /// 输入的文本.
    /// </summary>
    public EmoteText Text
    {
        get => (EmoteText)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <inheritdoc/>
    protected override void OnControlLoaded()
    {
        base.OnControlLoaded();
        if (Text is not null && (RichBlock?.Blocks.Count == 0))
        {
            InitializeContent();
        }
    }

    /// <inheritdoc/>
    protected override void OnControlUnloaded()
    {
        base.OnControlUnloaded();
        Gallery?.ItemsSource = null;
    }

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        EmoteTextBlock? instance = d as EmoteTextBlock;
        if (instance is not null)
        {
            instance.InitializeContent();
            instance.FlyoutRichBlock?.Blocks?.Clear();
        }
    }

    private void InitializeContent()
    {
        if (RichBlock is not null)
        {
            RichBlock.Blocks.Clear();
            Paragraph? para = null;
            if (Text is not null)
            {
                para = ParseText();
            }

            if (para is not null)
            {
                RichBlock.Visibility = Visibility.Visible;
                RichBlock.Blocks.Add(para);
            }
            else
            {
                RichBlock.Visibility = Visibility.Collapsed;
            }

            CheckTextTrim();
        }

        if (Text?.Pictures?.Count > 0)
        {
            ItemsRepeater? gallery = FindName("Gallery") as ItemsRepeater;
            if (gallery is not null)
            {
                gallery.ItemsSource = Text.Pictures;
                gallery.Visibility = Visibility.Visible;
            }
        }
        else if (Gallery is not null)
        {
            Gallery.ItemsSource = null;
            Gallery.Visibility = Visibility.Collapsed;
        }
    }

    private void OnTextTrimChanged(RichTextBlock sender, IsTextTrimmedChangedEventArgs args) =>
        CheckTextTrim();

    private Paragraph? ParseText()
    {
        if (string.IsNullOrEmpty(Text?.Text))
        {
            return null;
        }

        string text = Text.Text;
        Dictionary<string, AppImage>? emotes = Text.Emotes;
        Paragraph para = new();

        Regex emojiRegex = new(@"(\[.*?\])");
        string[] splitContents =
        [
            .. emojiRegex.Split(text).Where(p => p.Length > 0),
        ];
        foreach (string content in splitContents)
        {
            if (emojiRegex.IsMatch(content))
            {
                if (
                    EmojiHelper.TryGetEmoji(content, out EmojiItem? localEmoji)
                    && localEmoji?.Uri is not null
                )
                {
                    InlineUIContainer inlineCon = new();
                    Image img = new()
                    {
                        Width = 20,
                        Height = 20,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(2, 0, 2, -4),
                    };
                    BitmapImage bitmap = new(localEmoji.Uri) { DecodePixelWidth = 40 };
                    img.Source = bitmap;
                    inlineCon.Child = img;
                    para.Inlines.Add(inlineCon);
                }
                else if (
                    emotes is not null
                    && emotes.TryGetValue(content, out AppImage? emoji)
                    && emoji?.Uri is not null
                )
                {
                    InlineUIContainer inlineCon = new();
                    Image img = new()
                    {
                        Width = 20,
                        Height = 20,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(2, 0, 2, -4),
                    };
                    BitmapImage bitmap = new(emoji.Uri) { DecodePixelWidth = 40 };
                    img.Source = bitmap;
                    inlineCon.Child = img;
                    para.Inlines.Add(inlineCon);
                }
                else
                {
                    para.Inlines.Add(new Run { Text = content });
                }
            }
            else
            {
                para.Inlines.Add(new Run { Text = content });
            }
        }

        return para;
    }

    private void CheckTextTrim()
    {
        OverflowButton.Visibility = RichBlock.IsTextTrimmed
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void OnImageTapped(object sender, TappedRoutedEventArgs e)
    {
        AppImage? image = (sender as Image)?.DataContext as AppImage;
        if (image is not null && Text?.Pictures is not null)
        {
            new GalleryWindow(image, [.. Text.Pictures]).Activate();
        }
    }

    private void OnOverflowFlyoutOpened(object sender, object e)
    {
        if (FlyoutRichBlock.Blocks.Count > 0)
        {
            return;
        }

        Paragraph? para = ParseText();
        if (para is not null)
        {
            FlyoutRichBlock.Blocks.Add(para);
        }
    }
}
