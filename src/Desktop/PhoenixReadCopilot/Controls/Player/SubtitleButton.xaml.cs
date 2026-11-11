using Microsoft.UI.Xaml.Controls;
using Richasy.BiliKernel.Models.Subtitle;

namespace PhoenixReadCopilot.Controls.Player;

/// <summary>
/// 字幕按钮.
/// </summary>
public sealed partial class SubtitleButton : SubtitleControlBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SubtitleButton"/> class.
    /// </summary>
    public SubtitleButton()
    {
        InitializeComponent();
    }

    protected override void OnControlUnloaded() => SubtitleView.ItemsSource = null;

    private void OnMetaChanged(ItemsView sender, ItemsViewSelectionChangedEventArgs args)
    {
        SubtitleMeta? meta = sender.SelectedItem as SubtitleMeta;
        if (meta is not null)
        {
            if (ViewModel.SelectedMeta != meta)
            {
                ViewModel.ChangeSubtitleCommand.Execute(meta);
            }
        }
        else
        {
            ViewModel.DeselectSubtitleCommand.Execute(null);
        }
    }

    private void OnFlyoutOpened(object sender, object e)
    {
        if (ViewModel?.Metas is null || ViewModel.SelectedMeta is null)
        {
            SubtitleView.Select(-1);
            return;
        }

        int index = ViewModel.Metas.ToList().IndexOf(ViewModel.SelectedMeta);
        if (index == -1)
        {
            return;
        }

        SubtitleView.Select(index);
    }
}
