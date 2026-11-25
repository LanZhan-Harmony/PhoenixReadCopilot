using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.Helpers.Animations;
using PhoenixReadCopilot.ViewModels.Components;
using WinUI3Package;

namespace PhoenixReadCopilot.Controls.Player;

/// <summary>
/// 视频推荐区域.
/// </summary>
internal sealed partial class VideoRecommendSection : UserControl
{
    public VideoPlayerRecommendSectionDetailViewModel? ViewModel
    {
        get => (VideoPlayerRecommendSectionDetailViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(VideoPlayerRecommendSectionDetailViewModel),
        typeof(VideoRecommendSection),
        new PropertyMetadata(null)
    );

    public VideoRecommendSection()
    {
        InitializeComponent();
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
            PlayerRecommendedVideoItemTemplate.ClearImage(element);
        }
    }
}
