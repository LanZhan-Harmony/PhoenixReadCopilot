using System.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.Items;
using PhoenixReadCopilot.ViewModels.View;

namespace PhoenixReadCopilot.Controls.User;

/// <summary>
/// 用户空间左侧 Section 导航选择控件.
/// </summary>
public sealed partial class UserSideSectionView : UserControlBase
{
    private bool _isUpdating;

    public UserSideSectionView()
    {
        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override void OnControlLoaded()
    {
        if (ViewModel != null)
        {
            ViewModel.SectionInitialized += OnSectionInitialized;
        }
        SectionView.SelectionChanged += OnSectionSelectionChanged;
        CheckSectionSelection();
    }

    /// <inheritdoc/>
    protected override void OnControlUnloaded()
    {
        if (ViewModel != null)
        {
            ViewModel.SectionInitialized -= OnSectionInitialized;
        }
        SectionView.SelectionChanged -= OnSectionSelectionChanged;
    }

    /// <inheritdoc/>
    protected override void OnViewModelChanged(UserSpacePageViewModel? oldViewModel, UserSpacePageViewModel? newViewModel)
    {
        if (oldViewModel != null)
        {
            oldViewModel.SectionInitialized -= OnSectionInitialized;
            oldViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        if (newViewModel != null)
        {
            newViewModel.SectionInitialized += OnSectionInitialized;
            newViewModel.PropertyChanged += OnViewModelPropertyChanged;
            CheckSectionSelection();
        }
    }

    private void OnSectionInitialized(object? sender, EventArgs e) => CheckSectionSelection();

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(UserSpacePageViewModel.SelectedSection))
        {
            CheckSectionSelection();
        }
    }

    private void CheckSectionSelection()
    {
        if (_isUpdating || ViewModel == null)
        {
            return;
        }

        try
        {
            _isUpdating = true;
            if (ViewModel.SelectedSection != null)
            {
                int index = ViewModel.Sections.IndexOf(ViewModel.SelectedSection);
                if (index >= 0)
                {
                    if (!SectionView.IsSelected(index))
                    {
                        SectionView.Select(index);
                    }
                }
                else
                {
                    SectionView.DeselectAll();
                }
            }
            else
            {
                SectionView.DeselectAll();
            }
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private void OnSectionSelectionChanged(ItemsView sender, ItemsViewSelectionChangedEventArgs args)
    {
        if (_isUpdating)
        {
            return;
        }

        if (sender.SelectedItem is UserSpaceSectionItemViewModel item)
        {
            ViewModel?.SelectSection(item);
        }
    }
}
