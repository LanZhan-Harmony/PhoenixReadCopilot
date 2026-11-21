using System.Windows.Input;
using Microsoft.UI.Xaml;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.Controls.Player;

/// <summary>
/// 收藏按钮.
/// </summary>
public sealed partial class FavoriteButton : LayoutUserControlBase
{
    /// <summary>
    /// <see cref="IsFavorited"/> 的依赖属性.
    /// </summary>
    public static readonly DependencyProperty IsFavoritedProperty = DependencyProperty.Register(
        nameof(IsFavorited),
        typeof(bool),
        typeof(FavoriteButton),
        new PropertyMetadata(null)
    );

    /// <summary>
    /// <see cref="Tip"/> 的依赖属性.
    /// </summary>
    public static readonly DependencyProperty TipProperty = DependencyProperty.Register(
        nameof(FavoriteButton),
        typeof(string),
        typeof(FavoriteButton),
        new PropertyMetadata(null)
    );

    /// <summary>
    /// <see cref="Command"/> 的依赖属性.
    /// </summary>
    public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
        nameof(Command),
        typeof(ICommand),
        typeof(FavoriteButton),
        new PropertyMetadata(null)
    );

    /// <summary>
    /// Initializes a new instance of the <see cref="FavoriteButton"/> class.
    /// </summary>
    public FavoriteButton()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 是否已收藏.
    /// </summary>
    public bool IsFavorited
    {
        get => (bool)GetValue(IsFavoritedProperty);
        set => SetValue(IsFavoritedProperty, value);
    }

    /// <summary>
    /// 提示文本.
    /// </summary>
    public string Tip
    {
        get => (string)GetValue(TipProperty);
        set => SetValue(TipProperty, value);
    }

    /// <summary>
    /// 命令.
    /// </summary>
    public ICommand Command
    {
        get => (ICommand)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }
}
