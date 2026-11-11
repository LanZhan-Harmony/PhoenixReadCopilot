using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.UI;

namespace PhoenixReadCopilot.Controls.Danmaku;

/// <summary>
/// 弹幕发送选项.
/// </summary>
public sealed partial class DanmakuSendOptions : DanmakuControlBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DanmakuSendOptions"/> class.
    /// </summary>
    public DanmakuSendOptions()
    {
        InitializeComponent();
    }

    protected override void OnControlUnloaded() => ColorRepeater.ItemsSource = null;

    private void OnSizeItemClick(object sender, RoutedEventArgs e) =>
        ViewModel.IsStandardSize = (sender as FrameworkElement).Tag.ToString() == "Standard";

    private void OnColorItemTapped(object sender, TappedRoutedEventArgs e) =>
        ViewModel.Color = (Color)(sender as FrameworkElement).DataContext;
}
