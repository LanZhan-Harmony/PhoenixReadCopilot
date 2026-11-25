using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using PhoenixReadCopilot.ViewModels.Core;
using Windows.UI;

namespace PhoenixReadCopilot.Controls.Danmaku;

/// <summary>
/// 弹幕发送选项.
/// </summary>
internal sealed partial class DanmakuSendOptions : UserControl
{
    public DanmakuViewModel? ViewModel
    {
        get => (DanmakuViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(DanmakuViewModel),
        typeof(DanmakuSendOptions),
        new PropertyMetadata(null)
    );

    public DanmakuSendOptions()
    {
        InitializeComponent();
    }

    private void OnSizeItemClick(object sender, RoutedEventArgs e) =>
        ViewModel.IsStandardSize = (sender as FrameworkElement).Tag.ToString() == "Standard";

    private void OnColorItemTapped(object sender, TappedRoutedEventArgs e) =>
        ViewModel.Color = (Color)(sender as FrameworkElement).DataContext;
}
