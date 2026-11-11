using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using PhoenixReadCopilot.Core.Enums;
using Windows.Foundation;

namespace PhoenixReadCopilot.Controls.Core;

/// <summary>
/// 播放器.
/// </summary>
public sealed partial class PhoenixReadPlayer
{
    private void MeasureTransportTriggerRect()
    {
        if (_transportControl is FrameworkElement frameEle)
        {
            double width = frameEle.ActualWidth;
            double height = frameEle.ActualHeight;

            // 测量 frameEle 相对于 Player 的位置.
            Point transform = frameEle
                .TransformToVisual(_overlayContainer)
                .TransformPoint(new Point(0, 0));

            _transportControlTriggerRect = new Rect(
                (int)transform.X,
                (int)transform.Y,
                (int)width,
                (int)height
            );
        }
    }

    private void ArrangeSubtitleSize()
    {
        if (_subtitlePresenter is null)
        {
            return;
        }

        DispatcherQueue.TryEnqueue(
            DispatcherQueuePriority.Low,
            () =>
            {
                const int initialWidth = 1280;
                const int initialFontSize = 28;
                const int initialPaddingLeft = 16;
                const int initialPaddingTop = 12;

                // 根据实际宽度调整字幕大小，线性增长，每增加100px，字体大小增加3，横向边距增加8，纵向边距增加6.
                double width = ActualWidth;
                _subtitlePresenter.FontSize = Math.Max(
                    12,
                    initialFontSize + ((width - initialWidth) / 100 * 1.5)
                );
                double horizontalPadding = Math.Max(
                    8,
                    initialPaddingLeft + ((width - initialWidth) / 100 * 2.5)
                );
                double verticalPadding = Math.Max(
                    8,
                    initialPaddingTop + ((width - initialWidth) / 100 * 1)
                );
                _subtitlePresenter.Padding = new Thickness(
                    horizontalPadding,
                    verticalPadding,
                    horizontalPadding,
                    verticalPadding
                );
            }
        );
    }

    private void CheckTransportControlVisibility(PointerRoutedEventArgs? args = null)
    {
        bool isManual = _settings.MTCBehavior == MTCBehavior.Manual;
        if (
            isManual
            || _transportControl is null
            || ViewModel.IsPlayerDataLoading
            || ViewModel.IsPlayerInitializing
        )
        {
            return;
        }

        DispatcherQueue.TryEnqueue(() =>
        {
            if (args is not null)
            {
                if (_transportControl is not null)
                {
                    _lastRelativeMTCPoint = args.GetCurrentPoint(_transportControl).Position;
                }

                _lastPointerPoint = args.GetCurrentPoint(_overlayContainer).Position;
            }

            if (ViewModel.IsDanmakuInputFocused)
            {
                return;
            }

            if (_lastPointerPoint is null)
            {
                SetTransportVisibility(false);
                return;
            }

            bool isInStayArea =
                _lastRelativeMTCPoint is not null
                && _lastRelativeMTCPoint.Value is
                {
                    X: >= 0,
                    Y: >= 0
                }
                && _lastRelativeMTCPoint.Value.X <= _transportControl.ActualWidth
                && _lastRelativeMTCPoint.Value.Y <= _transportControl.ActualHeight;

            bool shouldShow = isInStayArea;
            if (!isInStayArea)
            {
                if (_transportControl.Visibility == Visibility.Collapsed && _mtcStayTime >= 2)
                {
                    // 原本已处于隐藏状态且鼠标未活动，保持隐藏，切集流转时不闪现弹出
                    shouldShow = false;
                }
                else if (ViewModel.IsPaused)
                {
                    shouldShow = true;
                }
                else if (_mtcStayTime < 2)
                {
                    shouldShow = true;
                }
            }

            SetTransportVisibility(shouldShow);
        });
    }

    private void SetTransportVisibility(bool isVisible)
    {
        if (_transportControl is null)
        {
            return;
        }

        Visibility visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        ViewModel?.CheckBottomProgressVisibility(!isVisible);
        if (_transportControl.Visibility == visibility)
        {
            return;
        }

        _transportControl.Visibility = visibility;
        MeasureTransportTriggerRect();
    }

    private void OnCursorTimerTick(object? sender, object e)
    {
        _cursorStayTime += 0.5;
        _mtcStayTime += 0.5;

        if (
            _lastPressedTime is not null
            && DateTimeOffset.Now - _lastPressedTime > TimeSpan.FromSeconds(1)
            && !_isHolding
        )
        {
            _isHolding = true;
            ToggleTripleSpeed(true);
        }

        if (
            _lastRightArrowPressedTime is not null
            && DateTimeOffset.Now - _lastRightArrowPressedTime > TimeSpan.FromSeconds(1)
            && !_isHolding
        )
        {
            _isHolding = true;
            ToggleTripleSpeed(true);
        }

        CheckTransportControlVisibility();
        if (
            _cursorStayTime >= 2
            && _transportControl is not null
            && _transportControl.Visibility == Visibility.Collapsed
            && ViewModel is
            {
                IsPaused: false,
                IsPlayerDataLoading: false
            }
        )
        {
            if (!_isCursorDisposed)
            {
                _overlayContainer.HideCursor();
                _isCursorDisposed = true;
            }

            _cursorStayTime = 0;
        }
    }

    private void RestoreCursor()
    {
        _cursorStayTime = 0;
        if (_isCursorDisposed)
        {
            _overlayContainer.ShowCursor();
            _isCursorDisposed = false;
        }
    }

    private void OnInteractionControlManipulationCompleted(
        object sender,
        ManipulationCompletedRoutedEventArgs e
    )
    {
        // 检查短剧垂直手势切集:
        // 向上滑 (Y轴向上位移，_manipulationDeltaY > 0) -> 下一集
        // 向下滑 (Y轴向下位移，_manipulationDeltaY < 0) -> 上一集
        if (
            Math.Abs(_manipulationDeltaY) > 60
            && Math.Abs(_manipulationDeltaY) > Math.Abs(_manipulationDeltaX) * 1.2
        )
        {
            if (_manipulationDeltaY > 0)
            {
                ViewModel?.PlayNextCommand?.Execute(null);
            }
            else
            {
                ViewModel?.PlayPreviousCommand?.Execute(null);
            }
        }

        _manipulationVolume = 0;
        _manipulationProgress = 0;
        _manipulationDeltaX = 0;
        _manipulationDeltaY = 0;
        _manipulationType = PlayerManipulationType.None;

        if (_manipulationBeforeIsPlay && ViewModel.IsPaused && !ViewModel.IsBuffering)
        {
            ViewModel.TogglePlayPauseCommand.Execute(null);
        }

        _manipulationBeforeIsPlay = false;
    }

    private void OnInteractionControlPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        PointerPoint point = e.GetCurrentPoint((UIElement)sender);
        int delta = point.Properties.MouseWheelDelta;
        if (delta == 0)
        {
            return;
        }

        // 限制两次切集的最短间隔（500ms），防止惯性滚轮飞速连跳
        if (DateTimeOffset.Now - _lastWheelTriggerTime < TimeSpan.FromMilliseconds(500))
        {
            e.Handled = true;
            return;
        }

        _wheelDeltaAccumulator += delta;

        if (_wheelDeltaAccumulator >= 120)
        {
            _wheelDeltaAccumulator = 0;
            _lastWheelTriggerTime = DateTimeOffset.Now;
            ViewModel?.PlayPreviousCommand?.Execute(null);
            e.Handled = true;
        }
        else if (_wheelDeltaAccumulator <= -120)
        {
            _wheelDeltaAccumulator = 0;
            _lastWheelTriggerTime = DateTimeOffset.Now;
            ViewModel?.PlayNextCommand?.Execute(null);
            e.Handled = true;
        }
    }

    private void OnInteractionControlManipulationDelta(
        object sender,
        ManipulationDeltaRoutedEventArgs e
    )
    {
        if (ViewModel.IsBuffering)
        {
            return;
        }

        _manipulationDeltaX += e.Delta.Translation.X;
        _manipulationDeltaY -= e.Delta.Translation.Y;
        if (Math.Abs(_manipulationDeltaX) > 15 || Math.Abs(_manipulationDeltaY) > 15)
        {
            if (_manipulationType == PlayerManipulationType.None)
            {
                bool isHorizontal = Math.Abs(_manipulationDeltaX) >= Math.Abs(_manipulationDeltaY);
                if (isHorizontal)
                {
                    _manipulationType = PlayerManipulationType.Progress;
                }
            }

            if (_manipulationType == PlayerManipulationType.Progress)
            {
                double progress =
                    _manipulationProgress + (_manipulationDeltaX * _manipulationUnitLength);
                if (progress > ViewModel.Duration)
                {
                    progress = ViewModel.Duration;
                }
                else if (progress < 0)
                {
                    progress = 0;
                }

                ViewModel.SeekCommand.Execute(progress);
            }
        }
    }

    private void OnInteractionControlManipulationStarted(
        object sender,
        ManipulationStartedRoutedEventArgs e
    )
    {
        if (ViewModel.IsPlayerDataLoading || ViewModel.IsBuffering || ViewModel.IsFailed)
        {
            return;
        }

        _manipulationProgress = ViewModel.Position;
        _manipulationVolume = ViewModel.Volume;

        // 点击事件先于手势事件，当该事件触发时，可能已经切换了播放状态.
        _manipulationBeforeIsPlay = ViewModel.IsPaused;
        if (ViewModel.Duration > 0)
        {
            // 获取单位像素对应的时长
            double unit = ViewModel.Duration / ActualWidth;
            _manipulationUnitLength = unit / 1.5;
        }
    }

    private void OnRootPointerCanceled(object sender, PointerRoutedEventArgs e)
    {
        _lastPressedTime = null;
        _lastRightArrowPressedTime = null;
        if (_isHolding)
        {
            ToggleTripleSpeed(false);
        }

        HandlePointerEvent(e, !ViewModel.IsDanmakuInputFocused);
    }

    private void OnRootPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        PointerPoint point = e.GetCurrentPoint((UIElement)sender);
        if (point.Properties.PointerUpdateKind == PointerUpdateKind.RightButtonPressed)
        {
            FlyoutShowOptions options = new() { Position = point.Position };
            _contextFlyout.ShowAt(_interactionControl, options);
            return;
        }

        _isPointerHasPressed = true;
        if (
            e.Pointer.PointerDeviceType == PointerDeviceType.Mouse
            || e.Pointer.PointerDeviceType == PointerDeviceType.Touch
        )
        {
            _lastPressedTime = DateTimeOffset.Now;
        }
    }

    private void OnRootPointerExited(object sender, PointerRoutedEventArgs e)
    {
        _lastPressedTime = null;
        _lastRightArrowPressedTime = null;
        if (_isHolding)
        {
            ToggleTripleSpeed(false);
        }

        HandlePointerEvent(e, !ViewModel.IsDanmakuInputFocused);
    }

    private void OnRootPointerEntered(object sender, PointerRoutedEventArgs e) =>
        HandlePointerEvent(e);

    private void OnRootPointerMoved(object sender, PointerRoutedEventArgs e) =>
        HandlePointerEvent(e);

    private void OnRootPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (
            _isPointerHasPressed != true
            || e.GetCurrentPoint((UIElement)sender).Properties.PointerUpdateKind
                == PointerUpdateKind.RightButtonReleased
        )
        {
            return;
        }

        _isPointerHasPressed = null;
        _lastPressedTime = null;
        _lastRightArrowPressedTime = null;
        if (_isHolding)
        {
            ToggleTripleSpeed(false);
        }
        else
        {
            if (_isDoubleTapped)
            {
                _isDoubleTapped = false;
                return;
            }

            bool isManual = _settings.MTCBehavior == MTCBehavior.Manual;
            if (isManual)
            {
                if (_transportControl is not null)
                {
                    SetTransportVisibility(_transportControl.Visibility == Visibility.Collapsed);
                }
            }
            else if (!_isHolding)
            {
                ViewModel?.TogglePlayPauseCommand.Execute(null);
            }
        }
    }
}
