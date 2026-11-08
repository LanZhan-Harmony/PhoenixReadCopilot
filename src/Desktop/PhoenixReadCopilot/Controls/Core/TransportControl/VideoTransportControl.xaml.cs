using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

namespace PhoenixReadCopilot.Controls.Core;

/// <summary>
/// 视频播放器控件.
/// </summary>
public sealed partial class VideoTransportControl : PlayerControlBase
{
    private DispatcherTimer? _seekDebounceTimer;
    private double _pendingSeekPosition = -1;

    /// <summary>
    /// <see cref="LeftContent"/> 的依赖属性.
    /// </summary>
    public static readonly DependencyProperty LeftContentProperty = DependencyProperty.Register(
        nameof(LeftContent),
        typeof(object),
        typeof(VideoTransportControl),
        new PropertyMetadata(default)
    );

    /// <summary>
    /// <see cref="MiddleContent"/> 的依赖属性.
    /// </summary>
    public static readonly DependencyProperty MiddleContentProperty = DependencyProperty.Register(
        nameof(MiddleContent),
        typeof(object),
        typeof(VideoTransportControl),
        new PropertyMetadata(default)
    );

    /// <summary>
    /// <see cref="RightContent"/> 的依赖属性.
    /// </summary>
    public static readonly DependencyProperty RightContentProperty = DependencyProperty.Register(
        nameof(RightContent),
        typeof(object),
        typeof(VideoTransportControl),
        new PropertyMetadata(default)
    );

    /// <summary>
    /// Initializes a new instance of the <see cref="VideoTransportControl"/> class.
    /// </summary>
    public VideoTransportControl()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 左侧内容.
    /// </summary>
    public object LeftContent
    {
        get => (object)GetValue(LeftContentProperty);
        set => SetValue(LeftContentProperty, value);
    }

    /// <summary>
    /// 中部内容.
    /// </summary>
    public object MiddleContent
    {
        get => (object)GetValue(MiddleContentProperty);
        set => SetValue(MiddleContentProperty, value);
    }

    /// <summary>
    /// 右侧内容.
    /// </summary>
    public object RightContent
    {
        get => (object)GetValue(RightContentProperty);
        set => SetValue(RightContentProperty, value);
    }

    private void OnVolumeSliderValueChanged(object sender, RangeBaseValueChangedEventArgs e) =>
        ViewModel?.SetVolumeCommand.Execute(Convert.ToInt32(e.NewValue));

    private void OnProgressSliderValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (ViewModel is null || ViewModel.IsPlayerDataLoading)
        {
            return;
        }

        // 常规播放微小步进，不触发防抖和底层 Seek
        if (Math.Abs(e.NewValue - ViewModel.Position) < 1.5)
        {
            return;
        }

        _pendingSeekPosition = e.NewValue;

        if (_seekDebounceTimer is null)
        {
            _seekDebounceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(160)
            };
            _seekDebounceTimer.Tick += (_, _) =>
            {
                _seekDebounceTimer.Stop();
                CommitPendingSeek();
            };
        }

        _seekDebounceTimer.Stop();
        _seekDebounceTimer.Start();
    }

    private void OnProgressSliderPointerReleased(object sender, PointerRoutedEventArgs e) =>
        CommitPendingSeek();

    private void OnProgressSliderPointerCaptureLost(object sender, PointerRoutedEventArgs e) =>
        CommitPendingSeek();

    private void CommitPendingSeek()
    {
        _seekDebounceTimer?.Stop();
        if (_pendingSeekPosition >= 0)
        {
            var target = _pendingSeekPosition;
            _pendingSeekPosition = -1;
            ViewModel?.SeekCommand.Execute(Convert.ToInt32(target));
        }
    }

    private void OnSpeedSliderValueChanged(object sender, RangeBaseValueChangedEventArgs e) =>
        ViewModel?.SetSpeedCommand.Execute(Math.Round(e.NewValue, 1));
}
