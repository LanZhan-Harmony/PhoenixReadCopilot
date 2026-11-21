using System.ComponentModel;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.Extensions;
using PhoenixReadCopilot.Helpers.Animations;
using PhoenixReadCopilot.ViewModels.View;
using WinUI3Package;

namespace PhoenixReadCopilot.Controls.User;

/// <summary>
/// 用户空间主体内容展示控件（基于 DataTemplateSelector 按需实例化各模式视图）.
/// </summary>
public sealed partial class UserMainBody : UserControlBase
{
    private ScrollViewer? _activeScrollViewer;
    private RevealFocusPanel? _activeRevealFocusPanel;

    public UserMainBody()
    {
        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override void OnControlLoaded()
    {
        if (ViewModel != null)
        {
            ViewModel.DataUpdated += OnDataUpdated;
        }
    }

    /// <inheritdoc/>
    protected override void OnControlUnloaded()
    {
        if (ViewModel != null)
        {
            ViewModel.DataUpdated -= OnDataUpdated;
        }

        if (_activeScrollViewer != null)
        {
            _activeScrollViewer.ViewChanged -= OnScrollViewerViewChanged;
            _activeScrollViewer = null;
        }
    }

    /// <inheritdoc/>
    protected override void OnViewModelChanged(
        UserSpacePageViewModel? oldViewModel,
        UserSpacePageViewModel? newViewModel
    )
    {
        if (oldViewModel != null)
        {
            oldViewModel.DataUpdated -= OnDataUpdated;
            oldViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        if (newViewModel != null)
        {
            newViewModel.DataUpdated += OnDataUpdated;
            newViewModel.PropertyChanged += OnViewModelPropertyChanged;
            UpdateTemplate();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(UserSpacePageViewModel.SelectedSection)
            or nameof(UserSpacePageViewModel.IsReviewViewVisible)
            or nameof(UserSpacePageViewModel.IsVideoViewVisible)
            or nameof(UserSpacePageViewModel.IsCircleViewVisible)
            or nameof(UserSpacePageViewModel.IsRelationViewVisible))
        {
            UpdateTemplate();
        }
    }

    private void UpdateTemplate()
    {
        if (Resources.TryGetValue("SectionSelector", out object? selectorObj)
            && selectorObj is UserSectionTemplateSelector selector
            && ViewModel?.SelectedSection is { } section)
        {
            DataTemplate? targetTemplate = selector.SelectTemplate(section);
            if (MainContentControl.ContentTemplate != targetTemplate)
            {
                MainContentControl.ContentTemplate = targetTemplate;
            }
        }
    }

    private void OnRevealFocusPanelLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is RevealFocusPanel panel)
        {
            _activeRevealFocusPanel = panel;
        }
    }

    private void OnScrollViewerLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is ScrollViewer sv)
        {
            _activeScrollViewer = sv;
            sv.ViewChanged += OnScrollViewerViewChanged;
        }
    }

    private void OnScrollViewerUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is ScrollViewer sv)
        {
            sv.ViewChanged -= OnScrollViewerViewChanged;
            if (_activeScrollViewer == sv)
            {
                _activeScrollViewer = null;
            }
        }
    }

    private void OnScrollViewerViewChanged(object? sender, ScrollViewerViewChangedEventArgs args)
    {
        if (sender is ScrollViewer sv)
        {
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                if (
                    sv.ExtentHeight - sv.ViewportHeight - sv.VerticalOffset <= 240
                    && ViewModel is { IsLoading: false, HasMore: true }
                )
                {
                    _ = ViewModel.LoadMoreAsync();
                }
            });
        }
    }

    private async void OnDataUpdated(object? sender, EventArgs e)
    {
        await Task.Delay(300);
        CheckContentFill();
    }

    private void CheckContentFill()
    {
        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            if (ViewModel is not { IsLoading: false, HasMore: true })
            {
                return;
            }

            if (_activeScrollViewer != null && _activeScrollViewer.ScrollableHeight <= 240)
            {
                _ = ViewModel.LoadMoreAsync();
            }
        });
    }

    private void VideoItemsRepeater_ElementPrepared(
        ItemsRepeater sender,
        ItemsRepeaterElementPreparedEventArgs args
    )
    {
        if (args.Element is FrameworkElement element)
        {
            if (_activeRevealFocusPanel != null)
            {
                RevealFocusPanel.SetAttachToPanel(element, _activeRevealFocusPanel);
            }

            CompositionFactory.DisableStandardReposition(element);
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                CompositionFactory.SetStandardReposition(element);
            });
        }
    }

    private void VideoItemsRepeater_ElementClearing(
        ItemsRepeater sender,
        ItemsRepeaterElementClearingEventArgs args
    )
    {
        if (args.Element is FrameworkElement element)
        {
            CompositionFactory.DisableStandardReposition(element);
            UserVideoItemTemplate.ClearImage(element);
        }
    }

    private void ReviewItemsRepeater_ElementPrepared(
        ItemsRepeater sender,
        ItemsRepeaterElementPreparedEventArgs args
    )
    {
        if (args.Element is FrameworkElement element && _activeRevealFocusPanel != null)
        {
            RevealFocusPanel.SetAttachToPanel(element, _activeRevealFocusPanel);
        }
    }

    private void ReviewItemsRepeater_ElementClearing(
        ItemsRepeater sender,
        ItemsRepeaterElementClearingEventArgs args
    )
    {
        if (args.Element is FrameworkElement element)
        {
            UserReviewItemTemplate.ClearImage(element);
        }
    }

    private void CircleItemsRepeater_ElementPrepared(
        ItemsRepeater sender,
        ItemsRepeaterElementPreparedEventArgs args
    )
    {
        if (args.Element is FrameworkElement element)
        {
            if (_activeRevealFocusPanel != null)
            {
                RevealFocusPanel.SetAttachToPanel(element, _activeRevealFocusPanel);
            }

            CompositionFactory.DisableStandardReposition(element);
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                CompositionFactory.SetStandardReposition(element);
            });
        }
    }

    private void CircleItemsRepeater_ElementClearing(
        ItemsRepeater sender,
        ItemsRepeaterElementClearingEventArgs args
    )
    {
        if (args.Element is FrameworkElement element)
        {
            CompositionFactory.DisableStandardReposition(element);
            CircleFeedItemTemplate.ClearImage(element);
        }
    }

    private void RelationItemsRepeater_ElementPrepared(
        ItemsRepeater sender,
        ItemsRepeaterElementPreparedEventArgs args
    )
    {
        if (args.Element is FrameworkElement element)
        {
            if (_activeRevealFocusPanel != null)
            {
                RevealFocusPanel.SetAttachToPanel(element, _activeRevealFocusPanel);
            }

            CompositionFactory.DisableStandardReposition(element);
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                CompositionFactory.SetStandardReposition(element);
            });
        }
    }

    private void RelationItemsRepeater_ElementClearing(
        ItemsRepeater sender,
        ItemsRepeaterElementClearingEventArgs args
    )
    {
        if (args.Element is FrameworkElement element)
        {
            CompositionFactory.DisableStandardReposition(element);
        }
    }
}
