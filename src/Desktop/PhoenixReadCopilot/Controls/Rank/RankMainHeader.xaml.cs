using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.Items;
using PhoenixReadCopilot.ViewModels.View;

namespace PhoenixReadCopilot.Controls.Rank;

/// <summary>
/// 榜单页面主标题.
/// </summary>
internal sealed partial class RankMainHeader : UserControl
{
    private bool _isUpdatingBoardItems;

    public RankPageViewModel? ViewModel
    {
        get => (RankPageViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(RankPageViewModel),
        typeof(RankMainHeader),
        new PropertyMetadata(null)
    );

    /// <summary>
    /// Initializes a new instance of the <see cref="RankMainHeader"/> class.
    /// </summary>
    public RankMainHeader()
    {
        InitializeComponent();
        Loaded += (_, _) => OnControlLoaded();
        Unloaded += (_, _) => OnControlUnloaded();
    }

    private void OnControlLoaded()
    {
        BoardSelector.SelectionChanged += OnBoardSelectionChanged;
        if (ViewModel is not null)
        {
            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        UpdateBoardItems();
    }

    private void OnControlUnloaded()
    {
        BoardSelector.SelectionChanged -= OnBoardSelectionChanged;
        if (ViewModel is not null)
        {
            ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }
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
        if (ViewModel?.SelectedSection is not RankSectionItemViewModel { LeaderboardType: { } })
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
                ) in RankPageViewModel.Channels
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
        if (match is not null && BoardSelector.SelectedItem != match)
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
            _ = ViewModel?.ChangeChannelAsync(channelName);
        }
    }
}
