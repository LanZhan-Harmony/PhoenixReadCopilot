using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.Controls.Player;

/// <summary>
/// PGC播放器格式按钮.
/// </summary>
public sealed partial class PgcPlayerFormatButton : PgcPlayerPageControlBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PgcPlayerFormatButton"/> class.
    /// </summary>
    public PgcPlayerFormatButton()
    {
        InitializeComponent();
    }

    protected override void OnControlUnloaded() => FormatView.ItemsSource = null;

    private void OnFlyoutOpened(object sender, object e)
    {
        int index = ViewModel.Formats.ToList().IndexOf(ViewModel.SelectedFormat);
        if (index != -1)
        {
            FormatView.Select(index);
        }
    }

    private void OnFormatSelectionChanged(ItemsView sender, ItemsViewSelectionChangedEventArgs args)
    {
        PlayerFormatItemViewModel? format = sender.SelectedItem as PlayerFormatItemViewModel;
        ViewModel.ChangeFormatCommand.Execute(format);
    }
}
