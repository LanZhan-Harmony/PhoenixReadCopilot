using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.Items;
using PhoenixReadCopilot.ViewModels.View;

namespace PhoenixReadCopilot.Controls.User;

/// <summary>
/// 用户空间左侧 Section 导航选择控件.
/// </summary>
internal sealed partial class UserSideSectionView : UserControl
{
    private bool _isUpdating;

    /// <summary>
    /// 视图模型.
    /// </summary>
    public UserSpacePageViewModel? ViewModel
    {
        get => (UserSpacePageViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(UserSpacePageViewModel),
        typeof(UserSideSectionView),
        new PropertyMetadata(null, (d, e) => ((UserSideSectionView)d).OnViewModelChanged(e.OldValue as UserSpacePageViewModel, e.NewValue as UserSpacePageViewModel))
    );

    public UserSideSectionView()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not null)
        {
            ViewModel.SectionInitialized += OnSectionInitialized;
        }
        CheckSectionSelection();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not null)
        {
            ViewModel.SectionInitialized -= OnSectionInitialized;
        }
    }

    private void OnViewModelChanged(
        UserSpacePageViewModel? oldViewModel,
        UserSpacePageViewModel? newViewModel
    )
    {
        if (oldViewModel is not null)
        {
            oldViewModel.SectionInitialized -= OnSectionInitialized;
            oldViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        if (newViewModel is not null)
        {
            if (IsLoaded)
            {
                newViewModel.SectionInitialized += OnSectionInitialized;
            }
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
        if (_isUpdating || ViewModel is null)
        {
            return;
        }

        try
        {
            _isUpdating = true;
            if (ViewModel.SelectedSection is not null)
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

    private void OnSectionSelectionChanged(
        ItemsView sender,
        ItemsViewSelectionChangedEventArgs args
    )
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
