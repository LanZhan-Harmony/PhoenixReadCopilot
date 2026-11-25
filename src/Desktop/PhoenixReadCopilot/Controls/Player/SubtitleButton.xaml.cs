using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.Core.Models.Subtitle;
using PhoenixReadCopilot.ViewModels.Core;

namespace PhoenixReadCopilot.Controls.Player;

/// <summary>
/// 字幕按钮.
/// </summary>
internal sealed partial class SubtitleButton : UserControl
{
    public SubtitleViewModel? ViewModel
    {
        get => (SubtitleViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(SubtitleViewModel),
        typeof(SubtitleButton),
        new PropertyMetadata(null)
    );

    public SubtitleButton()
    {
        InitializeComponent();
    }

    private void OnMetaChanged(ItemsView sender, ItemsViewSelectionChangedEventArgs args)
    {
        SubtitleMeta? meta = sender.SelectedItem as SubtitleMeta;
        if (meta is not null)
        {
            if (ViewModel.SelectedMeta != meta)
            {
                _ = ViewModel.ChangeSubtitleAsync(meta);
            }
        }
        else
        {
            ViewModel.DeselectSubtitle();
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
