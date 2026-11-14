using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Core.Models.Media;
using PhoenixReadCopilot.Core.Models.Settings;
using PhoenixReadCopilot.Extensions;
using PhoenixReadCopilot.Pages.Overlay;
using PhoenixReadCopilot.ViewModels.Core;
using Windows.Graphics;
using Windows.System;
using Windows.Win32;
using Windows.Win32.Foundation;
using WinUIEx;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.Forms;

/// <summary>
/// 播放器窗口.
/// </summary>
public sealed partial class PlayerWindow : WinUIEx.WindowEx, IPlayerHostWindow, ITipWindow
{
    private readonly AppViewModel _appViewModel;
    private readonly AppSettings _settings;

    private const int WindowMinWidth = 640;
    private const int WindowMinHeight = 480;
    private readonly InputActivationListener _inputActivationListener;
    private bool _isFirstActivated = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerWindow"/> class.
    /// </summary>
    public PlayerWindow()
    {
        _settings = Ioc.Default.GetRequiredService<ISettingsService>().Settings;
        _appViewModel = Ioc.Default.GetRequiredService<AppViewModel>();
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(TitleBar);
        Title = Strings.Resources.AppName;
        AppWindow.SetIcon("Assets/Logo/Icon.ico");
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Standard;
        MinWidth = WindowMinWidth;
        MinHeight = WindowMinHeight;
        _appViewModel.Windows.Add(this);
        _inputActivationListener = InputActivationListener.GetForWindowId(AppWindow.Id);
        _inputActivationListener.InputActivationChanged += OnInputActivationChanged;
        Activated += OnActivated;
        Closed += OnClosed;
    }

    /// <summary>
    /// 打开视频.
    /// </summary>
    public void OpenVideo(VideoSnapshot snapshot)
    {
        Activate();
        MainFrame.Navigate(typeof(VideoPlayerPage), snapshot);
    }

    /// <summary>
    /// 打开短剧内容.
    /// </summary>
    public void OpenDrama(string dramaId)
    {
        Activate();
        MainFrame.Navigate(typeof(VideoPlayerPage), dramaId);
    }

    /// <summary>
    /// 打开视频.
    /// </summary>
    public void OpenVideo((List<VideoInformation> videos, VideoSnapshot snapshot) data)
    {
        Activate();
        MainFrame.Navigate(typeof(VideoPlayerPage), data);
    }

    /// <inheritdoc/>
    public void EnterPlayerHostMode(PlayerDisplayMode mode)
    {
        TitleBar.Visibility =
            mode == PlayerDisplayMode.FullScreen ? Visibility.Collapsed : Visibility.Visible;
        TitleBar.IsBackButtonVisible =
            mode == PlayerDisplayMode.FullWindow || mode == PlayerDisplayMode.CompactOverlay;
        TitleBar.IsBackButtonEnabled =
            mode == PlayerDisplayMode.FullWindow || mode == PlayerDisplayMode.CompactOverlay;
        if (MainFrame.Content is VideoPlayerPage vPage)
        {
            TitleBar.Title = vPage.ViewModel.Title;
            vPage.EnterPlayerHostMode();
        }
    }

    /// <inheritdoc/>
    public void ExitPlayerHostMode()
    {
        TitleBar.Visibility = Visibility.Visible;
        TitleBar.Title = Strings.Resources.AppName;
        TitleBar.IsBackButtonVisible = false;
        if (MainFrame.Content is VideoPlayerPage vPage)
        {
            vPage.ExitPlayerHostMode();
        }
    }

    /// <inheritdoc/>
    public async Task ShowTipAsync(string text, InfoType type = InfoType.Error)
    {
        TipPopup popup = new() { Text = text };
        TipContainer.Visibility = Visibility.Visible;
        TipContainer.Children.Add(popup);
        await popup.ShowAsync(type);
        TipContainer.Children.Remove(popup);
        TipContainer.Visibility = Visibility.Collapsed;
    }

    private PointInt32 GetSavedWindowPosition()
    {
        int left = _settings.PlayerWindowPositionLeft;
        int top = _settings.PlayerWindowPositionTop;
        return new PointInt32(left, top);
    }

    private void OnActivated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState != WindowActivationState.Deactivated)
        {
            _appViewModel.ActivatedWindow = this;
        }

        if (!_isFirstActivated)
        {
            return;
        }

        MoveAndResize();
        bool isMaximized = _settings.IsPlayerWindowMaximized;
        if (isMaximized)
        {
            (AppWindow.Presenter as OverlappedPresenter).Maximize();
        }

        ElementTheme localTheme = _settings.AppTheme.ToElementTheme();
        _appViewModel.ChangeThemeCommand.Execute(localTheme);
        _isFirstActivated = false;
    }

    private void OnClosed(object sender, WindowEventArgs e)
    {
        ClosePlayer();

        Activated -= OnActivated;
        Closed -= OnClosed;

        _appViewModel.Windows.Remove(this);
        GlobalHook.Stop();
        SaveCurrentWindowStats();
    }

    private void OnInputActivationChanged(
        InputActivationListener sender,
        InputActivationListenerActivationChangedEventArgs args
    )
    {
        bool isDeactivated = sender.State == InputActivationState.Deactivated;
        if (isDeactivated)
        {
            GlobalHook.KeyDown -= OnWindowKeyDown;
            GlobalHook.KeyUp -= OnWindowKeyUp;
            GlobalHook.Stop();
        }
        else
        {
            GlobalHook.Start();
            GlobalHook.KeyDown += OnWindowKeyDown;
            GlobalHook.KeyUp += OnWindowKeyUp;
        }
    }

    private bool TryBackToDefaultMode()
    {
        if (MainFrame.Content is VideoPlayerPage vPage)
        {
            vPage.ViewModel.Player.BackToDefaultModeCommand.Execute(null);
            return true;
        }

        return false;
    }

    private bool TryTogglePlayPauseIfInPlayer()
    {
        if (MainFrame.Content is VideoPlayerPage vPage)
        {
            return vPage.ViewModel.Player.TryTogglePlayPause();
        }

        return false;
    }

    private bool TryMarkRightArrowPressedTime()
    {
        if (MainFrame.Content is VideoPlayerPage vPage)
        {
            return vPage.MarkRightArrowPressed();
        }

        return false;
    }

    private bool TryCancelRightArrow()
    {
        if (MainFrame.Content is VideoPlayerPage vPage)
        {
            return vPage.CancelRightArrow();
        }

        return false;
    }

    private void ClosePlayer()
    {
        if (MainFrame.Content is VideoPlayerPage vPage)
        {
            vPage.ViewModel.CleanCommand.Execute(null);
        }
    }

    private void OnWindowKeyDown(object? sender, PlayerKeyboardEventArgs e)
    {
        if (e.Key == VirtualKey.Space || e.Key == VirtualKey.Pause)
        {
            if (e.Key == VirtualKey.Space)
            {
                object focusEle = FocusManager.GetFocusedElement(MainFrame.XamlRoot);
                if (focusEle is TextBox)
                {
                    return;
                }
            }

            e.Handled = TryTogglePlayPauseIfInPlayer();
        }
        else if (e.Key == VirtualKey.Right)
        {
            e.Handled = TryMarkRightArrowPressedTime();
        }
    }

    private void OnWindowKeyUp(object? sender, PlayerKeyboardEventArgs e)
    {
        if (e.Key == VirtualKey.Right)
        {
            e.Handled = TryCancelRightArrow();
        }
    }

    private RectInt32 GetRenderRect(RectInt32 workArea)
    {
        double scaleFactor = this.GetDpiForWindow() / 96d;
        double previousWidth = _settings.PlayerWindowWidth;
        double previousHeight = _settings.PlayerWindowHeight;
        int width = Convert.ToInt32(previousWidth * scaleFactor);
        int height = Convert.ToInt32(previousHeight * scaleFactor);

        // Ensure the window is not larger than the work area.
        if (height > workArea.Height - 20)
        {
            height = workArea.Height - 20;
        }

        PointInt32 lastPoint = GetSavedWindowPosition();
        bool isZeroPoint = lastPoint is { X: 0, Y: 0 };
        bool isValidPosition = lastPoint.X >= workArea.X && lastPoint.Y >= workArea.Y;
        double left = isZeroPoint || !isValidPosition ? (workArea.Width - width) / 2d : lastPoint.X;
        double top =
            isZeroPoint || !isValidPosition ? (workArea.Height - height) / 2d : lastPoint.Y;
        return new RectInt32(Convert.ToInt32(left), Convert.ToInt32(top), width, height);
    }

    private void MoveAndResize()
    {
        PointInt32 lastPoint = GetSavedWindowPosition();
        DisplayArea displayArea = DisplayArea.GetFromPoint(lastPoint, DisplayAreaFallback.Primary)
            ?? DisplayArea.Primary;
        RectInt32 rect = GetRenderRect(displayArea.WorkArea);
        AppWindow.MoveAndResize(rect);
    }

    private void SaveCurrentWindowStats()
    {
        int left = AppWindow.Position.X;
        int top = AppWindow.Position.Y;
        BOOL isMaximized = PInvoke.IsZoomed(new HWND(this.GetWindowHandle()));
        _settings.IsPlayerWindowMaximized = isMaximized;

        if (!isMaximized)
        {
            _settings.PlayerWindowPositionLeft = left;
            _settings.PlayerWindowPositionTop = top;

            if (Height >= WindowMinHeight && Width >= WindowMinWidth)
            {
                _settings.PlayerWindowHeight = Height;
                _settings.PlayerWindowWidth = Width;
            }
        }
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        PointerPoint point = e.GetCurrentPoint((UIElement)sender);
        if (point.Properties.IsXButton1Pressed || point.Properties.IsXButton2Pressed)
        {
            e.Handled = true;
            TryBackToDefaultMode();
        }
    }

    private void OnBackRequested(Microsoft.UI.Xaml.Controls.TitleBar sender, object args)
    {
        TryBackToDefaultMode();
    }
}
