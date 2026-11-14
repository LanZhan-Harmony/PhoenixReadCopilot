using System.ComponentModel;
using System.Windows.Input;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using PhoenixReadCopilot.Controls.Danmaku;
using PhoenixReadCopilot.Controls.Player;
using Windows.System;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.Controls.Core;

/// <summary>
/// 红果播放器.
/// </summary>
public sealed partial class PhoenixReadPlayer
{
    private Func<DanmakuControlBase> _createDanmakuControlFunc;
    private Func<FrameworkElement> _createTransportControlFunc;
    private Func<SubtitlePresenter> _createSubtitleControlFunc;

    private CursorGrid _overlayContainer;
    private Panel _operationContainer;
    private DanmakuControlBase _danmakuControl;
    private FrameworkElement _transportControl;
    private SubtitlePresenter _subtitlePresenter;
    private ProgressBar _progressBar;
    private Rectangle _interactionControl;
    private StackPanel _notificationContainer;
    private LoadingWidget _loadingWidget;
    private EmptyHolder _failedHolder;
    private MenuFlyout _contextFlyout;

    /// <summary>
    /// 注入弹幕控件创建函数.
    /// </summary>
    public void InjectDanmakuControlFunc(Func<DanmakuControlBase> createDanmakuControlFunc) =>
        _createDanmakuControlFunc = createDanmakuControlFunc;

    /// <summary>
    /// 注入控制面板创建函数.
    /// </summary>
    public void InjectTransportControlFunc(Func<FrameworkElement> createTransportControlFunc) =>
        _createTransportControlFunc = createTransportControlFunc;

    /// <summary>
    /// 注入字幕控件创建函数.
    /// </summary>
    public void InjectSubtitleControlFunc(Func<SubtitlePresenter> createSubtitleControlFunc) =>
        _createSubtitleControlFunc = createSubtitleControlFunc;

    private void CreateOverlayContainer()
    {
        CursorGrid rootGrid = new();

        _danmakuControl = _createDanmakuControlFunc?.Invoke() ?? null;

        if (_danmakuControl is not null)
        {
            _danmakuControl.Margin = new Thickness(0);
            _danmakuControl.HorizontalAlignment = HorizontalAlignment.Stretch;
            _danmakuControl.VerticalAlignment = VerticalAlignment.Stretch;
            rootGrid.Children.Add(_danmakuControl);
        }

        _progressBar = new ProgressBar
        {
            Margin = new Thickness(0, 0, 0, 1),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Bottom,
            IsIndeterminate = ViewModel?.IsBuffering ?? false,
            Maximum = ViewModel?.Duration ?? 0,
            Value = ViewModel?.Position ?? 0,
            ShowPaused = ViewModel?.IsPaused ?? false,
            Visibility =
                ViewModel?.IsBottomProgressVisible ?? false
                    ? Visibility.Visible
                    : Visibility.Collapsed,
        };
        rootGrid.Children.Add(_progressBar);

        MenuFlyout contextFlyout = new();
        contextFlyout.Items.Add(
            new MenuFlyoutItem
            {
                Text = Strings.Resources.Screenshot,
                Command = ViewModel?.TakeScreenshotCommand,
                MinWidth = 160,
                Icon = new FluentIcons.WinUI.SymbolIcon
                {
                    Symbol = FluentIcons.Common.Symbol.Screenshot,
                },
            }
        );
        contextFlyout.Items.Add(
            new MenuFlyoutItem
            {
                Text = Strings.Resources.Reload,
                Command = ViewModel?.ReloadCommand,
                MinWidth = 160,
                Icon = new FluentIcons.WinUI.SymbolIcon
                {
                    Symbol = FluentIcons.Common.Symbol.ArrowClockwise,
                },
            }
        );

        _contextFlyout = contextFlyout;
        _interactionControl = new Rectangle
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Fill = new SolidColorBrush(Colors.Transparent),
            Visibility = Visibility.Visible,
        };
        HookInteractionControlEvents();
        rootGrid.Children.Add(_interactionControl);

        _notificationContainer = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top,
            Visibility = Visibility.Visible,
        };
        rootGrid.Children.Add(_notificationContainer);

        _operationContainer = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Bottom,
        };

        _subtitlePresenter = _createSubtitleControlFunc?.Invoke() ?? null;
        if (_subtitlePresenter is not null)
        {
            _subtitlePresenter.HorizontalAlignment = HorizontalAlignment.Stretch;
            _operationContainer.Children.Add(_subtitlePresenter);
        }

        _transportControl = _createTransportControlFunc?.Invoke() ?? null;
        if (_transportControl is not null)
        {
            _transportControl.MinHeight = 16;
            _transportControl.HorizontalAlignment = HorizontalAlignment.Stretch;
            _transportControl.VerticalAlignment = VerticalAlignment.Bottom;
            _transportControl.SizeChanged += (_, _) => MeasureTransportTriggerRect();
            _operationContainer.Children.Add(_transportControl);
        }

        rootGrid.Children.Add(_operationContainer);

        _loadingWidget = new LoadingWidget
        {
            Margin = new Thickness(8),
            Text = Strings.Resources.LoadingAndWait,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Visibility =
                ViewModel?.IsPlayerDataLoading ?? false ? Visibility.Visible : Visibility.Collapsed,
        };
        rootGrid.Children.Add(_loadingWidget);

        _failedHolder = new EmptyHolder
        {
            Title = Strings.Resources.FailedToPlay,
            Description = Strings.Resources.FailedToPlayDescription,
            Emoji = "😫",
            Visibility = ViewModel?.IsFailed ?? false ? Visibility.Visible : Visibility.Collapsed,
            ActionElement = new Button
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Command = ViewModel?.ReloadCommand,
                Content = Strings.Resources.Reload,
            },
        };
        rootGrid.Children.Add(_failedHolder);
        _overlayContainer = rootGrid;
        HookRootPointerEvents();
    }

    private void HandleViewModelPropertyChanged(PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.IsPaused))
        {
            _progressBar?.ShowPaused = ViewModel.IsPaused;
        }
        else if (e.PropertyName == nameof(ViewModel.IsBuffering))
        {
            _progressBar?.IsIndeterminate = ViewModel.IsBuffering;
        }
        else if (e.PropertyName == nameof(ViewModel.Duration))
        {
            _progressBar?.Maximum = ViewModel.Duration;
        }
        else if (e.PropertyName == nameof(ViewModel.Position))
        {
            _progressBar?.Value = ViewModel.Position;
        }
        else if (e.PropertyName == nameof(ViewModel.IsBottomProgressVisible))
        {
            _progressBar?.Visibility = ViewModel.IsBottomProgressVisible
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
        else if (e.PropertyName == nameof(ViewModel.IsFailed))
        {
            _notificationContainer?.Visibility = ViewModel.IsFailed
                ? Visibility.Collapsed
                : Visibility.Visible;

            _operationContainer?.Visibility = ViewModel.IsFailed
                ? Visibility.Collapsed
                : Visibility.Visible;

            _failedHolder?.Visibility = ViewModel.IsFailed
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
        else if (e.PropertyName == nameof(ViewModel.IsPlayerDataLoading))
        {
            _loadingWidget?.Visibility = ViewModel.IsPlayerDataLoading
                ? Visibility.Visible
                : Visibility.Collapsed;

            if (!ViewModel.IsPlayerDataLoading)
            {
                DispatcherQueue.TryEnqueue(ArrangeSubtitleSize);
            }
        }
        else if (e.PropertyName == nameof(ViewModel.IsDanmakuInputFocused))
        {
            DispatcherQueue.TryEnqueue(() => CheckTransportControlVisibility());
        }
    }

    private Grid CreateHiddenControlsContainer()
    {
        Grid grid = new();
        Button playPreviousBtn = CreateHiddenButton(VirtualKey.Up, ViewModel.PlayPreviousCommand);
        Button playNextBtn = CreateHiddenButton(VirtualKey.Down, ViewModel.PlayNextCommand);
        Button backwardBtn = CreateHiddenButton(VirtualKey.Left, ViewModel.BackwardSkipCommand);
        Button miniBtn = CreateHiddenButton(VirtualKey.F9, ViewModel.ToggleCompactOverlayCommand);
        Button fullBtn = CreateHiddenButton(VirtualKey.F11, ViewModel.ToggleFullScreenCommand);
        Button fullWindowBtn = CreateHiddenButton(
            VirtualKey.F10,
            ViewModel.ToggleFullWindowCommand
        );
        Button backBtn = CreateHiddenButton(VirtualKey.Escape, ViewModel.BackToDefaultModeCommand);
        grid.Children.Add(playPreviousBtn);
        grid.Children.Add(playNextBtn);
        grid.Children.Add(backwardBtn);
        grid.Children.Add(miniBtn);
        grid.Children.Add(fullBtn);
        grid.Children.Add(fullWindowBtn);
        grid.Children.Add(backBtn);

        Button CreateHiddenButton(
            VirtualKey key,
            ICommand command,
            VirtualKeyModifiers modifiers = VirtualKeyModifiers.None
        )
        {
            KeyboardAccelerator accelerator = new()
            {
                Key = key,
                Modifiers = modifiers,
                IsEnabled = true,
            };

            Button btn = new()
            {
                Background = new SolidColorBrush(Colors.Transparent),
                Width = 1,
                Height = 1,
                BorderThickness = new Thickness(0),
                IsTabStop = false,
                TabFocusNavigation = KeyboardNavigationMode.Once,
                TabNavigation = KeyboardNavigationMode.Once,
                XYFocusKeyboardNavigation = XYFocusKeyboardNavigationMode.Disabled,
                Command = command,
            };

            btn.KeyboardAccelerators.Add(accelerator);
            return btn;
        }

        return grid;
    }
}

/// <summary>
/// 光标网格.
/// </summary>
public partial class CursorGrid : Grid
{
    /// <summary>
    /// 隐藏光标.
    /// </summary>
    public void HideCursor() => ProtectedCursor?.Dispose();

    /// <summary>
    /// 显示光标.
    /// </summary>
    public void ShowCursor() =>
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
}
