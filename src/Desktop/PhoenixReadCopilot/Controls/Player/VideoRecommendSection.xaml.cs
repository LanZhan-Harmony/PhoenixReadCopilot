using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.Helpers.Animations;
using PhoenixReadCopilot.ViewModels.Components;
using WinUI3Package;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.Controls.Player;

/// <summary>
/// 视频推荐区域.
/// </summary>
public sealed partial class VideoRecommendSection : VideoRecommendSectionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VideoRecommendSection"/> class.
    /// </summary>
    public VideoRecommendSection()
    {
        InitializeComponent();
    }

    protected override void OnControlUnloaded() => VideoRepeater.ItemsSource = null;

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
            PlayerRecommendedVideoItemTemplate.ClearImage(element);
        }
    }
}

/// <summary>
/// 视频推荐区域基类.
/// </summary>
public abstract class VideoRecommendSectionBase
    : LayoutUserControlBase<VideoPlayerRecommendSectionDetailViewModel> { }
