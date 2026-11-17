using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.Helpers.Animations;
using WinUI3Package;
using WinUIKernel.Share;

namespace PhoenixReadCopilot.Controls.ViewLater;

/// <summary>
/// 稍后再看主体.
/// </summary>
public sealed partial class ViewLaterBody : ViewLaterPageControlBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ViewLaterBody"/> class.
    /// </summary>
    public ViewLaterBody()
    {
        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override void OnControlLoaded()
    {
        VideoScrollView.ViewChanged += OnViewChanged;
    }

    /// <inheritdoc/>
    protected override void OnControlUnloaded()
    {
        VideoScrollView.ViewChanged -= OnViewChanged;
    }

    private void OnViewChanged(object? sender, ScrollViewerViewChangedEventArgs args)
    {
        WinUIKernelShareExtensions.IsCardAnimationEnabled = !args.IsIntermediate;
    }

    private void VideoRepeater_ElementPrepared(
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

    private void VideoRepeater_ElementClearing(
        ItemsRepeater sender,
        ItemsRepeaterElementClearingEventArgs args
    )
    {
        if (args.Element is FrameworkElement element)
        {
            CompositionFactory.DisableStandardReposition(element);
            ViewLaterVideoItemTemplate.ClearImage(element);
        }
    }
}
