using System.Windows.Input;
using Microsoft.UI.Xaml;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.Controls.Player;

/// <summary>
/// 分享按钮.
/// </summary>
public sealed partial class ShareButton : LayoutUserControlBase
{
    /// <summary>
    /// <see cref="Command"/> 的依赖属性.
    /// </summary>
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

    /// <summary>
    /// 点击命令.
    /// </summary>
    public ICommand Command
    {
        get => (ICommand)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }
}
