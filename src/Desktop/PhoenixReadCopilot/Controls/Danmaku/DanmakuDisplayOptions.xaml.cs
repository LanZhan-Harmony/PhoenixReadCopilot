using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.Core;

namespace PhoenixReadCopilot.Controls.Danmaku;

/// <summary>
/// 弹幕显示选项.
/// </summary>
internal sealed partial class DanmakuDisplayOptions : UserControl
{
    public DanmakuViewModel? ViewModel
    {
        get => (DanmakuViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(DanmakuViewModel),
        typeof(DanmakuDisplayOptions),
        new PropertyMetadata(null)
    );

    public DanmakuDisplayOptions()
    {
        InitializeComponent();
    }
}
