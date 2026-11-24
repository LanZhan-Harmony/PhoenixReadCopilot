using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.Helpers.Animations;
using PhoenixReadCopilot.ViewModels.View;
using WinUI3Package;

namespace PhoenixReadCopilot.Controls.Filter;

/// <summary>
/// 娱乐索引主体.
/// </summary>
internal sealed partial class FilterIndexMainControl : UserControl
{
    public FilterPageViewModel? ViewModel
    {
        get => (FilterPageViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(FilterPageViewModel),
        typeof(FilterIndexMainControl),
        new PropertyMetadata(null, (d, e) =>
            ((FilterIndexMainControl)d).OnViewModelChanged(
                (FilterPageViewModel?)e.OldValue,
                (FilterPageViewModel?)e.NewValue
            )
        )
    );

    /// <summary>
    /// Initializes a new instance of the <see cref="FilterIndexMainControl"/> class.
    /// </summary>
    public FilterIndexMainControl()
    {
        InitializeComponent();
        Loaded += (_, _) => OnControlLoaded();
        Unloaded += (_, _) => OnControlUnloaded();
    }

    private void OnControlLoaded()
    {
        IndexScrollView.ViewChanged += OnViewChanged;
        IndexScrollView.SizeChanged += OnScrollViewSizeChanged;

        if (ViewModel is null)
        {
            return;
        }

        CheckIndexCount();
    }

    private void OnControlUnloaded()
    {
        ViewModel?.ItemsUpdated -= OnItemsUpdatedAsync;

        IndexRepeater.ItemsSource = null;
        IndexScrollView.ViewChanged -= OnViewChanged;
        IndexScrollView.SizeChanged -= OnScrollViewSizeChanged;
    }

    private void OnViewModelChanged(FilterPageViewModel? oldValue, FilterPageViewModel? newValue)
    {
        oldValue?.ItemsUpdated -= OnItemsUpdatedAsync;

        if (newValue is null)
        {
            return;
        }

        IndexScrollView?.ChangeView(0, 0, null, true);
        newValue.ItemsUpdated += OnItemsUpdatedAsync;
    }

    private async void OnItemsUpdatedAsync(object? sender, EventArgs e)
    {
        await Task.Delay(500);
        CheckIndexCount();
    }

    private void OnViewChanged(object? sender, ScrollViewerViewChangedEventArgs args)
    {
        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            if (
                IndexScrollView.ExtentHeight
                    - IndexScrollView.ViewportHeight
                    - IndexScrollView.VerticalOffset
                <= 240
            )
            {
                _ = ViewModel.RequestIndexAsync();
            }
        });
    }

    private void OnScrollViewSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (e.NewSize.Width > 100)
        {
            CheckIndexCount();
        }
    }

    private void CheckIndexCount()
    {
        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            if (IndexScrollView.ScrollableHeight <= 240 && ViewModel is not null)
            {
                _ = ViewModel.RequestIndexAsync();
            }
        });
    }

    private void ItemRepeater_ElementPrepared(
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

    private void ItemRepeater_ElementClearing(
        ItemsRepeater sender,
        ItemsRepeaterElementClearingEventArgs args
    )
    {
        if (args.Element is FrameworkElement element)
        {
            CompositionFactory.DisableStandardReposition(element);
            FilterVideoItemTemplate.ClearImage(element);
        }
    }
}
