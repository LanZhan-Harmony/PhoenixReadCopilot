using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.Controls.Core;

/// <summary>
/// 播放器通知控件.
/// </summary>
internal sealed partial class PlayerNotificationControl : UserControl
{
    public PlayerNotificationItemViewModel? ViewModel
    {
        get => (PlayerNotificationItemViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(PlayerNotificationItemViewModel),
        typeof(PlayerNotificationControl),
        new PropertyMetadata(null)
    );

    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerNotificationControl"/> class.
    /// </summary>
    public PlayerNotificationControl()
    {
        InitializeComponent();
        Loaded += (_, _) => ViewModel?.Start();
        Unloaded += (_, _) => ViewModel?.Cancel();
    }
}
