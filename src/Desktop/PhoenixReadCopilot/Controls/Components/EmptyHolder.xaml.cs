using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace PhoenixReadCopilot.Controls.Components;

/// <summary>
/// 占位符.
/// </summary>
internal sealed partial class EmptyHolder : UserControl
{
    /// <summary>
    /// Title.
    /// </summary>
    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title),
        typeof(string),
        typeof(EmptyHolder),
        new PropertyMetadata(null)
    );

    /// <summary>
    /// Description.
    /// </summary>
    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
        nameof(Description),
        typeof(string),
        typeof(EmptyHolder),
        new PropertyMetadata(null)
    );

    /// <summary>
    /// Emoji.
    /// </summary>
    public string Emoji
    {
        get => (string)GetValue(EmojiProperty);
        set => SetValue(EmojiProperty, value);
    }

    public static readonly DependencyProperty EmojiProperty = DependencyProperty.Register(
        nameof(Emoji),
        typeof(string),
        typeof(EmptyHolder),
        new PropertyMetadata(null)
    );

    /// <summary>
    /// The element that performs the action.
    /// </summary>
    public object ActionElement
    {
        get => GetValue(ActionElementProperty);
        set => SetValue(ActionElementProperty, value);
    }

    public static readonly DependencyProperty ActionElementProperty = DependencyProperty.Register(
        nameof(ActionElement),
        typeof(object),
        typeof(EmptyHolder),
        new PropertyMetadata(null)
    );

    /// <summary>
    /// Initializes a new instance of the <see cref="EmptyHolder"/> class.
    /// </summary>
    public EmptyHolder()
    {
        InitializeComponent();
    }
}
