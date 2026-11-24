using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace PhoenixReadCopilot.Controls.Player;

/// <summary>
/// 分享按钮.
/// </summary>
internal sealed partial class ShareButton : UserControl
{
    /// <summary>
    /// 点击命令.
    /// </summary>
    public ICommand Command
    {
        get => (ICommand)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
        nameof(Command),
        typeof(ICommand),
        typeof(ShareButton),
        new PropertyMetadata(null)
    );

    /// <summary>
    /// Initializes a new instance of the <see cref="ShareButton"/> class.
    /// </summary>
    public ShareButton()
    {
        InitializeComponent();
    }
}
