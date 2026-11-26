using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using PhoenixReadCopilot.Controls.Core;
using PhoenixReadCopilot.Controls.Danmaku;
using PhoenixReadCopilot.Controls.Player;
using PhoenixReadCopilot.Core.Contracts.Media;
using PhoenixReadCopilot.ViewModels.View;

namespace PhoenixReadCopilot.Pages.Overlay;

/// <summary>
/// 视频播放界面.
/// </summary>
internal sealed partial class VideoPlayerPage : Page
{
    /// <summary>
    /// 视图模型.
    /// </summary>
    public VideoPlayerPageViewModel ViewModel { get; } =
        Ioc.Default.GetRequiredService<VideoPlayerPageViewModel>();

    public VideoPlayerPage()
    {
        InitializeComponent();
        PhoenixReadPlayer.InjectDanmakuControlFunc(CreateDanmakuPanel);
        PhoenixReadPlayer.InjectTransportControlFunc(CreateTransportControl);
        // PhoenixReadPlayer.InjectSubtitleControlFunc(CreateSubtitleControl);
    }

    /// <summary>
    /// 进入播放器主持模式.
    /// </summary>
    public void EnterPlayerHostMode() =>
        VisualStateManager.GoToState(this, "PlayerHostState", false);

    /// <summary>
    /// 退出播放器主持模式.
    /// </summary>
    public void ExitPlayerHostMode() => VisualStateManager.GoToState(this, "DefaultState", false);

    /// <summary>
    /// 标记右箭头按下.
    /// </summary>
    /// <returns>是否成功.</returns>
    public bool MarkRightArrowPressed()
    {
        if (ViewModel.Player.IsPaused)
        {
            return false;
        }

        PhoenixReadPlayer.MarkRightArrowKeyPressedTime();
        return true;
    }

    /// <summary>
    /// 取消右箭头按下.
    /// </summary>
    /// <returns>是否成功.</returns>
    public bool CancelRightArrow()
    {
        if (ViewModel.Player.IsPaused)
        {
            return false;
        }

        PhoenixReadPlayer.CancelRightArrowKey();
        return true;
    }

    /// <summary>
    /// 浮层隐藏时暂停内部计时器，保留实例以便快速复用.
    /// </summary>
    public void OnOverlayHidden() => PhoenixReadPlayer.PauseTimers();

    /// <summary>
    /// 重新复用浮层时恢复内部状态与计时器.
    /// </summary>
    public void PrepareForReuse()
    {
        PhoenixReadPlayer.ViewModel ??= ViewModel.Player;
        PhoenixReadPlayer.ResumeTimers();
    }

    /// <inheritdoc/>
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        if (Frame.Tag is string and "PlayerWindow")
        {
            ViewModel.IsSeparatorWindowPlayer = true;
        }

        if (e.Parameter is IPlayableMedia playable)
        {
            _ = ViewModel.InitializePageAsync(playable);
        }
    }

    /// <inheritdoc/>
    protected override void OnNavigatingFrom(NavigatingCancelEventArgs e) => ViewModel.Clean();

    private FrameworkElement CreateDanmakuPanel() =>
        new VideoDanmakuPanel() { ViewModel = ViewModel.Danmaku };

    private FrameworkElement CreateTransportControl()
    {
        VideoPlayerTransportLeftPanel leftPanel = new() { ViewModel = ViewModel };
        DanmakuBox danmakuBox = new() { ViewModel = ViewModel.Danmaku };
        StackPanel rightPanel = new() { Orientation = Orientation.Horizontal };
        VideoPlayerFormatButton formatButton = new() { ViewModel = ViewModel };
        SubtitleButton subtitleButton = new() { ViewModel = ViewModel.Subtitle };
        rightPanel.Children.Add(formatButton);
        rightPanel.Children.Add(subtitleButton);
        danmakuBox.InputGotFocus += OnDanmakuInputGotFocus;
        danmakuBox.InputLostFocus += OnDanmakuInputLostFocus;
        return new VideoTransportControl()
        {
            LeftContent = leftPanel,
            RightContent = rightPanel,
            MiddleContent = danmakuBox,
            ViewModel = ViewModel.Player,
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(12),
            MaxWidth = 800,
        };
    }

    private void OnDanmakuInputGotFocus(object? sender, EventArgs e) =>
        ViewModel.Player.IsDanmakuInputFocused = true;

    private void OnDanmakuInputLostFocus(object? sender, EventArgs e) =>
        ViewModel.Player.IsDanmakuInputFocused = false;

    private SubtitlePresenter CreateSubtitleControl()
    {
        return new SubtitlePresenter
        {
            Margin = new Thickness(0, 0, 0, 16),
            ViewModel = ViewModel.Subtitle,
        };
    }
}
