using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace PhoenixReadCopilot.Controls.Player;

/// <summary>
/// 点赞按钮.
/// </summary>
internal sealed partial class LikeButton : UserControl
{
    /// <summary>
    /// 是否已点赞.
    /// </summary>
    public bool IsLiked
    {
        get => (bool)GetValue(IsLikedProperty);
        set => SetValue(IsLikedProperty, value);
    }

    public static readonly DependencyProperty IsLikedProperty = DependencyProperty.Register(
        nameof(IsLiked),
        typeof(bool),
        typeof(LikeButton),
        new PropertyMetadata(null)
    );

    /// <summary>
    /// 提示文本.
    /// </summary>
    public string Tip
    {
        get => (string)GetValue(TipProperty);
        set => SetValue(TipProperty, value);
    }

    public static readonly DependencyProperty TipProperty = DependencyProperty.Register(
        nameof(LikeButton),
        typeof(string),
        typeof(LikeButton),
        new PropertyMetadata(null)
    );

    /// <summary>
    /// 命令.
    /// </summary>
    public ICommand Command
    {
        get => (ICommand)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
        nameof(Command),
        typeof(ICommand),
        typeof(LikeButton),
        new PropertyMetadata(null)
    );

    /// <summary>
    /// Initializes a new instance of the <see cref="LikeButton"/> class.
    /// </summary>
    public LikeButton()
    {
        InitializeComponent();
    }
}
