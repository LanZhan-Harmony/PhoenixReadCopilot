using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.Helpers.Animations;
using PhoenixReadCopilot.ViewModels.Components;
using WinUI3Package;

namespace PhoenixReadCopilot.Controls.Search;

/// <summary>
/// 用户搜索分区详情控件.
/// </summary>
internal sealed partial class UserSectionDetailControl : UserSectionDetailControlBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserSectionDetailControl"/> class.
    /// </summary>
    public UserSectionDetailControl()
    {
        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override void OnControlLoaded()
    {
        ViewModel.ListUpdated += OnListUpdatedAsync;
        UserScrollView.ViewChanged += OnViewChanged;
        UserScrollView.SizeChanged += OnScrollViewSizeChanged;

        CheckUserCount();
    }

    /// <inheritdoc/>
    protected override void OnControlUnloaded()
    {
        UserRepeater.ItemsSource = null;
        ViewModel.ListUpdated -= OnListUpdatedAsync;
        UserScrollView.ViewChanged -= OnViewChanged;
        UserScrollView.SizeChanged -= OnScrollViewSizeChanged;
    }

    private async void OnListUpdatedAsync(object? sender, EventArgs e)
    {
        await Task.Delay(500);
        CheckUserCount();
    }

    private void OnViewChanged(object? sender, ScrollViewerViewChangedEventArgs args)
    {
        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            if (
                UserScrollView.ExtentHeight
                    - UserScrollView.ViewportHeight
                    - UserScrollView.VerticalOffset
                <= 240
            )
            {
                _ = ViewModel.LoadItemsAsync();
            }
        });
    }

    private void OnScrollViewSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (e.NewSize.Width > 100)
        {
            CheckUserCount();
        }
    }

    private void CheckUserCount()
    {
        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            if (UserScrollView.ScrollableHeight <= 240 && ViewModel is not null)
            {
                _ = ViewModel.LoadItemsAsync();
            }
        });
    }

    private void UserRepeater_ElementPrepared(
        ItemsRepeater sender,
        ItemsRepeaterElementPreparedEventArgs args
    )
    {
        if (args.Element is FrameworkElement element)
        {
            RevealFocusPanel.SetAttachToPanel(element, GridViewRevealFocusPanel);

            CompositionFactory.DisableStandardReposition(element);
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                CompositionFactory.SetStandardReposition(element);
            });
        }
    }

    private void UserRepeater_ElementClearing(
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

/// <summary>
/// 用户搜索分区详情控件基类.
/// </summary>
internal abstract class UserSectionDetailControlBase
    : LayoutUserControlBase<UserSearchSectionDetailViewModel> { }
