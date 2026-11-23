using System.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.Items;
using PhoenixReadCopilot.ViewModels.View;

namespace PhoenixReadCopilot.Controls.User;

/// <summary>
/// 用户空间主体顶部 Header 控件.
/// </summary>
internal sealed partial class UserMainHeader : UserControlBase
{
    private bool _isUpdating;

    public UserMainHeader()
    {
        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override void OnViewModelChanged(
        UserSpacePageViewModel? oldViewModel,
        UserSpacePageViewModel? newViewModel
    )
    {
        oldViewModel?.PropertyChanged -= OnViewModelPropertyChanged;

        if (newViewModel != null)
        {
            newViewModel.PropertyChanged += OnViewModelPropertyChanged;
            UpdateSubTabs();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(UserSpacePageViewModel.SelectedSection))
        {
            UpdateSubTabs();
        }
    }

    private void UpdateSubTabs()
    {
        if (_isUpdating)
        {
            return;
        }

        try
        {
            _isUpdating = true;
            SubTabSelector.Items.Clear();

            if (ViewModel?.SelectedSection is { } section && section.SubTabs.Count > 1)
            {
                SubTabSelector.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                SelectorBarItem? targetItem = null;
                foreach (UserSpaceSubTabItemViewModel subTab in section.SubTabs)
                {
                    SelectorBarItem item = new() { Text = subTab.Title, Tag = subTab };
                    SubTabSelector.Items.Add(item);

                    if (subTab == section.SelectedSubTab)
                    {
                        targetItem = item;
                    }
                }

                SubTabSelector.SelectedItem = targetItem ?? SubTabSelector.Items.FirstOrDefault();
            }
            else
            {
                SubTabSelector.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            }
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private void OnSubTabSelectionChanged(
        SelectorBar sender,
        SelectorBarSelectionChangedEventArgs args
    )
    {
        if (_isUpdating)
        {
            return;
        }

        if (sender.SelectedItem?.Tag is UserSpaceSubTabItemViewModel subTab)
        {
            ViewModel?.SelectSubTab(subTab);
        }
    }
}
