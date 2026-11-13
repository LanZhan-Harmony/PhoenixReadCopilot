using System.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Core.Models.Settings;
using PhoenixReadCopilot.ViewModels.Core;
using PhoenixReadCopilot.ViewModels.Items;
using Windows.Foundation;

namespace PhoenixReadCopilot.Controls.Core;

public sealed partial class PhoenixReadPlayer : PlayerControlBase
{
    private readonly AppSettings _settings;

    private Rect _transportControlTriggerRect;
    private DispatcherTimer? _cursorTimer;

    private bool _isHolding;
    private double _lastSpeed;
    private double _cursorStayTime;
    private double _mtcStayTime;
    private bool _isCursorDisposed;
    private Point? _lastPointerPoint;
    private Point? _lastRelativeMTCPoint;
    private bool _isDoubleTapped;

    private double _manipulationDeltaX;
    private double _manipulationDeltaY;
    private double _manipulationProgress;
    private double _manipulationVolume;
    private double _manipulationUnitLength;
    private bool _manipulationBeforeIsPlay;
    private PlayerManipulationType _manipulationType = PlayerManipulationType.None;
    private DateTimeOffset? _lastPressedTime;
    private DateTimeOffset? _lastRightArrowPressedTime;
    private bool? _isPointerHasPressed;
    private int _wheelDeltaAccumulator;
    private DateTimeOffset _lastWheelTriggerTime = DateTimeOffset.MinValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="PhoenixReadPlayer"/> class.
    /// </summary>
    public PhoenixReadPlayer()
    {
        _settings = Ioc.Default.GetRequiredService<ISettingsService>().Settings;
        InitializeComponent();
    }

    /// <summary>
    /// 获取当前是否有文本框获得焦点.
    /// </summary>
    /// <returns>结果.</returns>
    public bool IsTextBoxFocused() =>
        _overlayContainer is not null
        && FocusManager.GetFocusedElement(_overlayContainer.XamlRoot) is TextBox;

    /// <summary>
    /// 标记右箭头按下时间.
    /// </summary>
    public void MarkRightArrowKeyPressedTime()
    {
        if (_lastRightArrowPressedTime is not null)
        {
            return;
        }

        _lastRightArrowPressedTime = DateTimeOffset.Now;
    }

    /// <summary>
    /// 取消右箭头按下.
    /// </summary>
    public void CancelRightArrowKey()
    {
        if (_isHolding)
        {
            ToggleTripleSpeed(false);
        }
        else if (
            _lastPressedTime is null
            || DateTimeOffset.Now - _lastPressedTime < TimeSpan.FromSeconds(1)
        )
        {
            ViewModel.ForwardSkipCommand.Execute(null);
        }

        _lastRightArrowPressedTime = null;
    }

    /// <inheritdoc/>
    protected override void OnControlLoaded()
    {
        if (_cursorTimer is null)
        {
            _cursorTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(0.5) };
            _cursorTimer.Tick += OnCursorTimerTick;
        }

        if (ViewModel is null)
        {
            return;
        }

        PlayerPresenter.ViewModel = ViewModel;
        ViewModel.SetIsTextBoxFocusedFunc(IsTextBoxFocused);
        SizeChanged += OnSizeChanged;

        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            ArrangeSubtitleSize();
            MeasureTransportTriggerRect();
        });

        _cursorTimer?.Start();
    }

    /// <inheritdoc/>
    protected override void OnControlUnloaded()
    {
        UnHookEventsWithoutViewModel();
        if (ViewModel is not null)
        {
            ViewModel.RequestShowNotification -= OnRequestShowNotification;
            ViewModel.RequestCancelNotification -= OnRequestCancelNotification;
            ViewModel.PropertyChanged -= OnViewModelInnerPropertyChanged;
            ViewModel.Initialized -= OnViewModelInitialized;
        }

        ViewModel = null;
        PlayerPresenter.ViewModel = null;
    }

    /// <inheritdoc/>
    protected override void OnViewModelChanged(
        PlayerViewModelBase? oldValue,
        PlayerViewModelBase? newValue
    )
    {
        if (newValue is null)
        {
            PlayerPresenter.ViewModel = null;
            return;
        }

        if (oldValue is not null)
        {
            oldValue.PropertyChanged -= OnViewModelInnerPropertyChanged;
            oldValue.RequestShowNotification -= OnRequestShowNotification;
            oldValue.RequestCancelNotification -= OnRequestCancelNotification;
            oldValue.Initialized -= OnViewModelInitialized;
        }

        PlayerPresenter.ViewModel = newValue;
        newValue.PropertyChanged += OnViewModelInnerPropertyChanged;
        newValue.RequestShowNotification += OnRequestShowNotification;
        newValue.RequestCancelNotification += OnRequestCancelNotification;
        newValue.Initialized += OnViewModelInitialized;

        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            ArrangeSubtitleSize();
        });
    }

    private void HookRootPointerEvents()
    {
        _overlayContainer.PointerMoved += OnRootPointerMoved;
        _overlayContainer.PointerEntered += OnRootPointerEntered;
        _overlayContainer.PointerExited += OnRootPointerExited;
        _overlayContainer.PointerPressed += OnRootPointerPressed;
        _overlayContainer.PointerCanceled += OnRootPointerCanceled;
        _overlayContainer.PointerReleased += OnRootPointerReleased;
    }

    private void ToggleTripleSpeed(bool isQuick)
    {
        _isHolding = isQuick;
        if (isQuick)
        {
            _lastSpeed = ViewModel.Speed;
            ViewModel.SetSpeedCommand.Execute(3d);
        }
        else
        {
            _lastSpeed = _lastSpeed == 0 ? 1.0 : _lastSpeed;
            ViewModel.SetSpeedCommand.Execute(_lastSpeed);
            _lastSpeed = 0;
            _lastPressedTime = null;
        }
    }

    private void UnhookRootPointerEvents()
    {
        if (_overlayContainer is not null)
        {
            _overlayContainer.PointerMoved -= OnRootPointerMoved;
            _overlayContainer.PointerEntered -= OnRootPointerEntered;
            _overlayContainer.PointerExited -= OnRootPointerExited;
            _overlayContainer.PointerPressed -= OnRootPointerPressed;
            _overlayContainer.PointerCanceled -= OnRootPointerCanceled;
            _overlayContainer.PointerReleased -= OnRootPointerReleased;
        }
    }

    private void HookInteractionControlEvents()
    {
        _interactionControl.DoubleTapped += OnCoreDoubleTapped;
        _interactionControl.ManipulationMode =
            ManipulationModes.TranslateX | ManipulationModes.TranslateY;
        _interactionControl.ManipulationStarted += OnInteractionControlManipulationStarted;
        _interactionControl.ManipulationDelta += OnInteractionControlManipulationDelta;
        _interactionControl.ManipulationCompleted += OnInteractionControlManipulationCompleted;
        _interactionControl.PointerWheelChanged += OnInteractionControlPointerWheelChanged;
    }

    private void UnhookInteractionControlEvents()
    {
        if (_interactionControl is not null)
        {
            _interactionControl.DoubleTapped -= OnCoreDoubleTapped;
            _interactionControl.ManipulationStarted -= OnInteractionControlManipulationStarted;
            _interactionControl.ManipulationDelta -= OnInteractionControlManipulationDelta;
            _interactionControl.ManipulationCompleted -= OnInteractionControlManipulationCompleted;
            _interactionControl.PointerWheelChanged -= OnInteractionControlPointerWheelChanged;
        }
    }

    private void UnHookEventsWithoutViewModel()
    {
        SizeChanged -= OnSizeChanged;

        if (_cursorTimer is not null)
        {
            _cursorTimer.Tick -= OnCursorTimerTick;
            _cursorTimer.Stop();
            _cursorTimer = null;
        }

        UnhookInteractionControlEvents();
        UnhookRootPointerEvents();
    }

    private void HandlePointerEvent(
        PointerRoutedEventArgs e,
        bool forceHideTransportControls = false
    )
    {
        RestoreCursor();
        bool isManual = _settings.MTCBehavior == MTCBehavior.Manual;
        if (!isManual)
        {
            if (forceHideTransportControls)
            {
                _lastPointerPoint = null;
                _mtcStayTime = 0;
                SetTransportVisibility(false);
                return;
            }

            _mtcStayTime = 0;
            CheckTransportControlVisibility(e);
        }
    }

    private void OnRequestShowNotification(object? sender, PlayerNotificationItemViewModel e)
    {
        _notificationContainer.Visibility = Visibility.Visible;
        _notificationContainer.Children.Clear();
        PlayerNotificationControl control = new() { ViewModel = e };
        e.IsNotificationVisible = true;
        _notificationContainer.Children.Add(control);
    }

    private void OnRequestCancelNotification(object? sender, EventArgs e)
    {
        if (
            _notificationContainer is not null
            && _notificationContainer.Children.FirstOrDefault() is PlayerNotificationControl control
        )
        {
            control.ViewModel.CancelCommand.Execute(null);
            _notificationContainer.Children.Clear();
        }
    }

    private void OnViewModelInitialized(object? sender, EventArgs e)
    {
        if (_overlayContainer is not null)
        {
            return;
        }

        CreateOverlayContainer();

        RootGrid.Children.Add(_overlayContainer);
    }

    private void OnCoreDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        _isDoubleTapped = true;
        bool isManual = _settings.MTCBehavior == MTCBehavior.Manual;
        if (isManual)
        {
            ViewModel.TogglePlayPauseCommand.Execute(null);
        }
        else
        {
            ViewModel.ToggleFullScreenCommand.Execute(null);

            if (ViewModel.IsPaused)
            {
                ViewModel.TogglePlayPauseCommand.Execute(null);
            }
        }
    }

    private void OnViewModelInnerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_overlayContainer is not null)
        {
            HandleViewModelPropertyChanged(e);
        }

        if (ViewModel is not null && e.PropertyName == "IsPaused" && _transportControl is not null)
        {
            if (
                _transportControl.Visibility == Visibility.Collapsed
                && (ViewModel.IsPlayerDataLoading || _mtcStayTime >= 2)
            )
            {
                // 控制器此前已处于隐藏状态，在切集加载或未活动时保持隐藏，避免闪现弹出
                return;
            }

            SetTransportVisibility(ViewModel.IsPaused);
        }
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        MeasureTransportTriggerRect();
        ArrangeSubtitleSize();
    }
}
