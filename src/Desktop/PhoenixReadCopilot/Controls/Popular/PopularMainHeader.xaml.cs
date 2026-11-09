using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.Items;
using PhoenixReadCopilot.ViewModels.View;

namespace PhoenixReadCopilot.Controls.Popular;

/// <summary>
/// 流行视频与榜单页面主标题.
/// </summary>
public sealed partial class PopularMainHeader : PopularPageControlBase
{
    private bool _isUpdatingBoardItems;

    /// <summary>
    /// Initializes a new instance of the <see cref="PopularMainHeader"/> class.
    /// </summary>
    public PopularMainHeader()
    {
        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override void OnControlLoaded()
    {
        BoardSelector.SelectionChanged += OnBoardSelectionChanged;
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;

        UpdateBoardItems();
    }

    /// <inheritdoc/>
    protected override void OnControlUnloaded()
    {
        BoardSelector.SelectionChanged -= OnBoardSelectionChanged;
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(
        object? sender,
        System.ComponentModel.PropertyChangedEventArgs e
    )
    {
        if (e.PropertyName == nameof(ViewModel.SelectedSection))
        {
            UpdateBoardItems();
        }
        else if (e.PropertyName == nameof(ViewModel.CurrentChannel))
        {
            SyncSelectedChannel();
        }
    }

    private void UpdateBoardItems()
    {
        if (ViewModel?.SelectedSection is not PopularSectionItemViewModel { LeaderboardType: { } })
        {
            return;
        }

        try
        {
            _isUpdatingBoardItems = true;
            BoardSelector.SelectionChanged -= OnBoardSelectionChanged;
            BoardSelector.Items.Clear();
            SelectorBarItem? targetItem = null;

            foreach (
                (
                    string? title,
                    PhoenixReadApi.ShortDramaLeaderboardChannel channel
                ) in PopularPageViewModel.Channels
            )
            {
                SelectorBarItem item = new() { Text = title, Tag = channel.ToString() };
                BoardSelector.Items.Add(item);

                if (channel == ViewModel.CurrentChannel)
                {
                    targetItem = item;
                }
            }

            BoardSelector.SelectedItem = targetItem ?? BoardSelector.Items.FirstOrDefault();
        }
        finally
        {
            BoardSelector.SelectionChanged += OnBoardSelectionChanged;
            _isUpdatingBoardItems = false;
        }
    }

    private void SyncSelectedChannel()
    {
        if (_isUpdatingBoardItems)
        {
            return;
        }

        string currentTag = ViewModel.CurrentChannel.ToString();
        SelectorBarItem? match = BoardSelector.Items.FirstOrDefault(item =>
            item.Tag as string == currentTag
        );
        if (match != null && BoardSelector.SelectedItem != match)
        {
            try
            {
                _isUpdatingBoardItems = true;
                BoardSelector.SelectionChanged -= OnBoardSelectionChanged;
                BoardSelector.SelectedItem = match;
            }
            finally
            {
                BoardSelector.SelectionChanged += OnBoardSelectionChanged;
                _isUpdatingBoardItems = false;
            }
        }
    }

    private void OnBoardSelectionChanged(
        SelectorBar sender,
        SelectorBarSelectionChangedEventArgs args
    )
    {
        if (_isUpdatingBoardItems)
        {
            return;
        }

        if (sender.SelectedItem?.Tag is string channelName)
        {
            ViewModel?.ChangeChannelCommand.Execute(channelName);
        }
    }
}
