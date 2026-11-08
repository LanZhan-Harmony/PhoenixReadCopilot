using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.Helpers;
using PhoenixReadCopilot.ViewModels.Components;

namespace PhoenixReadCopilot.Controls.Components;

/// <summary>
/// 表情面板.
/// </summary>
public sealed partial class EmotePanel : UserControl
{
    /// <summary>
    /// 表情列表.
    /// </summary>
    public IReadOnlyList<EmojiItem> Emojis => EmojiHelper.Emojis;

    /// <summary>
    /// 视图模型.
    /// </summary>
    public EmoteModuleViewModel ViewModel { get; }

    /// <summary>
    /// 表情被选中.
    /// </summary>
    public event EventHandler<string>? ItemClick;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmotePanel"/> class.
    /// </summary>
    public EmotePanel()
    {
        ViewModel = Ioc.Default.GetRequiredService<EmoteModuleViewModel>();
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (EmoteView.ItemsSource is null)
        {
            EmoteView.ItemsSource = Emojis;
        }
    }

    private void OnEmoteViewItemInvoked(ItemsView sender, ItemsViewItemInvokedEventArgs args)
    {
        if (args.InvokedItem is EmojiItem emoji)
        {
            ItemClick?.Invoke(this, emoji.Key);
        }
    }
}
