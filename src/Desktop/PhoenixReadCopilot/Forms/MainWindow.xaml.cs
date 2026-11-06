using System.Diagnostics;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Models.Settings;
using PhoenixReadCopilot.Services;
using PhoenixReadCopilot.ViewModels.Core;
using Windows.Graphics;
using Windows.System;
using WinUIEx;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.Forms;

/// <summary>
/// 主窗口.
/// </summary>
public sealed partial class MainWindow : WinUIEx.WindowEx, IPlayerHostWindow, ITipWindow
{
    private readonly ISettingsService _settingsService;
    private readonly AppViewModel _appViewModel;
    private AppSettings _settings => _settingsService.Settings;

    private const int WindowMinWidth = 640;
    private const int WindowMinHeight = 480;
    private readonly InputActivationListener _inputActivationListener;
    private bool _isFirstActivated = true;
    private bool _shouldExit;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow()
    {
        _settingsService = Ioc.Default.GetRequiredService<ISettingsService>();
        _appViewModel = Ioc.Default.GetRequiredService<AppViewModel>();
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(RootLayout.GetMainTitleBar());
        Title = Strings.Resources.AppName;
        AppWindow.SetIcon("Assets/Logo/Icon.ico");
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        MinWidth = WindowMinWidth;
        MinHeight = WindowMinHeight;
        _appViewModel.Windows.Add(this);
        _inputActivationListener = InputActivationListener.GetForWindowId(AppWindow.Id);
        _inputActivationListener.InputActivationChanged += OnInputActivationChanged;
        Activated += OnActivated;
        Closed += OnClosed;
    }

    /// <inheritdoc/>
    public void EnterPlayerHostMode(PlayerDisplayMode mode) =>
        RootLayout.PrepareFullPlayerPresenter(mode);

    /// <inheritdoc/>
    public void ExitPlayerHostMode() => RootLayout.ExitFullPlayerPresenter();

    /// <inheritdoc/>
    public async Task ShowTipAsync(string text, InfoType type = InfoType.Error)
    {
        var popup = new TipPopup() { Text = text };
        TipContainer.Visibility = Visibility.Visible;
        TipContainer.Children.Add(popup);
        await popup.ShowAsync(type);
        TipContainer.Children.Remove(popup);
        TipContainer.Visibility = Visibility.Collapsed;
    }

    private PointInt32 GetSavedWindowPosition()
    {
        var left = _settings.MainWindowPositionLeft;
        var top = _settings.MainWindowPositionTop;
        return new PointInt32(left, top);
    }

    private void OnInputActivationChanged(
        InputActivationListener sender,
        InputActivationListenerActivationChangedEventArgs args
    )
    {
        var isDeactivated = sender.State == InputActivationState.Deactivated;
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
        var isMaximized = _settings.IsMainWindowMaximized;
        if (isMaximized)
        {
            (AppWindow.Presenter as OverlappedPresenter).Maximize();
        }

        var localTheme = _settings.AppTheme;
        _appViewModel.ChangeThemeCommand.Execute(localTheme);
        _isFirstActivated = false;
    }

    private async void OnClosed(object sender, WindowEventArgs e)
    {
        if (!_shouldExit)
        {
            var hideWhenClose = _settings.HideWhenCloseWindow;
            if (hideWhenClose)
            {
                e.Handled = true;
                this.Hide();
                SaveCurrentWindowStats();
                await _settingsService.FlushAsync();
                return;
            }

            _shouldExit = true;
            this.Hide();

            SaveCurrentWindowStats();
            await _settingsService.FlushAsync();

            _appViewModel.IsClosed = true;
            RootLayout.ViewModel.Back();
            foreach (var item in _appViewModel.Windows.ToList())
            {
                if (item is not MainWindow)
                {
                    item.Close();
                }
            }

            Activated -= OnActivated;
            Closed -= OnClosed;

            _appViewModel.Windows.Remove(this);
            GlobalHook.Stop();
            App.Current?.Exit();
        }
    }

    private void OnWindowKeyDown(object? sender, PlayerKeyboardEventArgs e)
    {
        if (e.Key == VirtualKey.Space || e.Key == VirtualKey.Pause)
        {
            if (e.Key == VirtualKey.Space)
            {
                var focusEle = FocusManager.GetFocusedElement(RootLayout.XamlRoot);
                if (focusEle is TextBox)
                {
                    return;
                }
            }

            e.Handled = RootLayout.TryTogglePlayPauseIfInPlayer();
        }
        else if (e.Key == VirtualKey.Right)
        {
            var focusEle = FocusManager.GetFocusedElement(RootLayout.XamlRoot);
            if (focusEle is TextBox)
            {
                return;
            }

            e.Handled = RootLayout.TryMarkRightArrowPressedTime();
        }
    }

    private void OnWindowKeyUp(object? sender, PlayerKeyboardEventArgs e)
    {
        if (e.Key == VirtualKey.Right)
        {
            var focusEle = FocusManager.GetFocusedElement(RootLayout.XamlRoot);
            if (focusEle is TextBox)
            {
                return;
            }

            e.Handled = RootLayout.TryCancelRightArrow();
        }
    }

    private RectInt32 GetRenderRect(RectInt32 workArea)
    {
        var scaleFactor = this.GetDpiForWindow() / 96d;
        var previousWidth = _settings.MainWindowWidth;
        var previousHeight = _settings.MainWindowHeight;
        var width = Convert.ToInt32(previousWidth * scaleFactor);
        var height = Convert.ToInt32(previousHeight * scaleFactor);

        // Ensure the window is not larger than the work area.
        if (height > workArea.Height - 20)
        {
            height = workArea.Height - 20;
        }

        var lastPoint = GetSavedWindowPosition();
        var isZeroPoint = lastPoint.X == 0 && lastPoint.Y == 0;
        var isValidPosition = lastPoint.X >= workArea.X && lastPoint.Y >= workArea.Y;
        var left = isZeroPoint || !isValidPosition ? (workArea.Width - width) / 2d : lastPoint.X;
        var top = isZeroPoint || !isValidPosition ? (workArea.Height - height) / 2d : lastPoint.Y;
        return new RectInt32(Convert.ToInt32(left), Convert.ToInt32(top), width, height);
    }

    private void MoveAndResize()
    {
        var lastPoint = GetSavedWindowPosition();
        var displayArea =
            DisplayArea.GetFromPoint(lastPoint, DisplayAreaFallback.Primary) ?? DisplayArea.Primary;
        var rect = GetRenderRect(displayArea.WorkArea);
        AppWindow.MoveAndResize(rect);
    }

    private void SaveCurrentWindowStats()
    {
        var left = AppWindow.Position.X;
        var top = AppWindow.Position.Y;
        var isMaximized = PInvoke.IsZoomed(new HWND(this.GetWindowHandle()));
        _settings.IsMainWindowMaximized = (bool)isMaximized;

        if (!isMaximized)
        {
            _settings.MainWindowPositionLeft = left;
            _settings.MainWindowPositionTop = top;

            if (Height >= WindowMinHeight && Width >= WindowMinWidth)
            {
                _settings.MainWindowHeight = Height;
                _settings.MainWindowWidth = Width;
            }
        }
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint((UIElement)sender);
        if (point.Properties.IsXButton1Pressed || point.Properties.IsXButton2Pressed)
        {
            e.Handled = true;
            if (RootLayout.TryBackToDefaultIfPlayerHostMode())
            {
                return;
            }

            if (RootLayout.ViewModel.IsOverlayOpen)
            {
                RootLayout.ViewModel.Back();
            }
        }
    }
}

internal static class GlobalHook
{
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;

    private static readonly HOOKPROC _procKeyboard = HookKeyboardCallback;
    private static UnhookWindowsHookExSafeHandle _keyboardHookID = new();
    public static event EventHandler<PlayerKeyboardEventArgs> KeyDown;
    public static event EventHandler<PlayerKeyboardEventArgs> KeyUp;

    public static void Start()
    {
        _keyboardHookID = SetHook(_procKeyboard, WINDOWS_HOOK_ID.WH_KEYBOARD_LL);
    }

    public static void Stop()
    {
        PInvoke.UnhookWindowsHookEx(new HHOOK(_keyboardHookID.DangerousGetHandle()));
    }

    private static UnhookWindowsHookExSafeHandle SetHook(HOOKPROC proc, WINDOWS_HOOK_ID hookId)
    {
        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule;
        return PInvoke.SetWindowsHookEx(
            hookId,
            proc,
            PInvoke.GetModuleHandle(curModule.ModuleName),
            0
        );
    }

    private static LRESULT HookKeyboardCallback(int nCode, WPARAM wParam, LPARAM lParam)
    {
        if (nCode >= 0 && wParam.Value == WM_KEYDOWN)
        {
            var vkCode = Marshal.ReadInt32(lParam);
            var isCtrlPressed = (PInvoke.GetKeyState((int)VirtualKey.Control) & 0x8000) != 0;
            var args = new PlayerKeyboardEventArgs(vkCode, isCtrlPressed);
            KeyDown?.Invoke(null, args);
            if (args.Handled)
            {
                return new LRESULT(1);
            }
        }
        else if (nCode >= 0 && wParam.Value == WM_KEYUP)
        {
            var vkCode = Marshal.ReadInt32(lParam);
            var isCtrlPressed = (PInvoke.GetKeyState((int)VirtualKey.Control) & 0x8000) != 0;
            var args = new PlayerKeyboardEventArgs(vkCode, isCtrlPressed);
            KeyUp?.Invoke(null, args);
            if (args.Handled)
            {
                return new LRESULT(1);
            }
        }

        return PInvoke.CallNextHookEx(HHOOK.Null, nCode, wParam, lParam);
    }
}

internal sealed class PlayerKeyboardEventArgs
{
    public PlayerKeyboardEventArgs(int keyCode, bool isControlPressed)
    {
        Key = (VirtualKey)keyCode;
        IsControlPressed = isControlPressed;
    }

    internal VirtualKey Key { get; }

    internal bool IsControlPressed { get; }

    internal bool Handled { get; set; }
}
