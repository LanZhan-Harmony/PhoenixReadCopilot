using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace PhoenixReadCopilot.Controls.Player;

/// <summary>
/// 收藏按钮.
/// </summary>
internal sealed partial class FavoriteButton : UserControl
{
    /// <summary>
    /// 是否已收藏.
    /// </summary>
    public bool IsFavorited
    {
        get => (bool)GetValue(IsFavoritedProperty);
        set => SetValue(IsFavoritedProperty, value);
    }

    public static readonly DependencyProperty IsFavoritedProperty = DependencyProperty.Register(
        nameof(IsFavorited),
        typeof(bool),
        typeof(FavoriteButton),
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
        nameof(FavoriteButton),
        typeof(string),
        typeof(FavoriteButton),
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
        typeof(FavoriteButton),
        new PropertyMetadata(null)
    );

    public FavoriteButton()
    {
        InitializeComponent();
    }
}
