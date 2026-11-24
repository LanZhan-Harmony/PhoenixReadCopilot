using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.Contracts.ViewModels;
using PhoenixReadCopilot.ViewModels.View;

namespace PhoenixReadCopilot.Controls.Player;

/// <summary>
/// 视频播放页侧边栏头部.
/// </summary>
internal sealed partial class VideoPlayerSideHeader : UserControl
{
    public VideoPlayerPageViewModel? ViewModel
    {
        get => (VideoPlayerPageViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(VideoPlayerPageViewModel),
        typeof(VideoPlayerSideHeader),
        new PropertyMetadata(null, (d, e) =>
            ((VideoPlayerSideHeader)d).OnViewModelChanged(
                (VideoPlayerPageViewModel?)e.OldValue,
                (VideoPlayerPageViewModel?)e.NewValue
            )
        )
    );

    public VideoPlayerSideHeader()
    {
        InitializeComponent();
        Loaded += (_, _) => OnControlLoaded();
        Unloaded += (_, _) => OnControlUnloaded();
    }

    private void OnControlLoaded()
    {
        Selector.SelectionChanged += OnSelectorChanged;
        if (ViewModel is null)
        {
            return;
        }

        InitializeChildPartitions();
    }

    private void OnControlUnloaded()
    {
        if (ViewModel is not null)
        {
            ViewModel.SectionInitialized -= OnViewModelSectionInitialized;
        }

        Selector.SelectionChanged -= OnSelectorChanged;
    }

    private void OnViewModelChanged(
        VideoPlayerPageViewModel? oldValue,
        VideoPlayerPageViewModel? newValue
    )
    {
        if (oldValue is not null)
        {
            oldValue.SectionInitialized -= OnViewModelSectionInitialized;
        }

        if (newValue is null)
        {
            return;
        }

        newValue.SectionInitialized += OnViewModelSectionInitialized;
        InitializeChildPartitions();
    }

    private void OnViewModelSectionInitialized(object? sender, EventArgs e) =>
        InitializeChildPartitions();

    private void OnSelectorChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        if (sender.SelectedItem is null)
        {
            return;
        }

        IPlayerSectionDetailViewModel? item =
            sender.SelectedItem.Tag as IPlayerSectionDetailViewModel;
        ViewModel.SelectSection(item);
    }

    private void InitializeChildPartitions()
    {
        Selector.Items.Clear();
        if (ViewModel.Sections is not null)
        {
            foreach (IPlayerSectionDetailViewModel item in ViewModel.Sections)
            {
                Selector.Items.Add(new SelectorBarItem() { Text = item.Title ?? null, Tag = item });
            }
        }

        Selector.SelectedItem = Selector.Items.FirstOrDefault(p =>
            p.Tag == ViewModel.SelectedSection
        );
    }
}
