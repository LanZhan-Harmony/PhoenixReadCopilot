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
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
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
    public void EnterPlayerHostMode(PlayerDisplayMode mode)
    {
        RootLayout.PrepareFullPlayerPresenter(mode);
        if (mode == PlayerDisplayMode.FullWindow || mode == PlayerDisplayMode.CompactOverlay)
        {
            SetTitleBar(RootLayout.GetSecondaryTitleBar());
        }
        else
        {
            SetTitleBar(null);
        }
    }

    /// <inheritdoc/>
    public void ExitPlayerHostMode()
    {
        RootLayout.ExitFullPlayerPresenter();
        SetTitleBar(RootLayout.GetMainTitleBar());
    }

    /// <inheritdoc/>
    public async Task ShowTipAsync(string text, InfoType type = InfoType.Error)
    {
        if (!DispatcherQueue.HasThreadAccess)
        {
            TaskCompletionSource tcs = new();
            DispatcherQueue.TryEnqueue(async () =>
            {
                try
                {
                    await ShowTipAsync(text, type);
                    tcs.TrySetResult();
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });
            await tcs.Task;
            return;
        }

        TipPopup popup = new() { Text = text };
        TipContainer.Visibility = Visibility.Visible;
        TipContainer.Children.Add(popup);
        await popup.ShowAsync(type);
        TipContainer.Children.Remove(popup);
        TipContainer.Visibility = Visibility.Collapsed;
    }

    private PointInt32 GetSavedWindowPosition()
    {
        int left = _settings.MainWindowPositionLeft;
        int top = _settings.MainWindowPositionTop;
        return new PointInt32(left, top);
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
        bool isMaximized = _settings.IsMainWindowMaximized;
        if (isMaximized)
        {
            (AppWindow.Presenter as OverlappedPresenter).Maximize();
        }

        ElementTheme localTheme = _settings.AppTheme;
        _appViewModel.ChangeThemeCommand.Execute(localTheme);
        _isFirstActivated = false;
    }

    private async void OnClosed(object sender, WindowEventArgs e)
    {
        if (!_shouldExit)
        {
            bool hideWhenClose = _settings.HideWhenCloseWindow;
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
            foreach (Window? item in _appViewModel.Windows.ToList())
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
                object focusEle = FocusManager.GetFocusedElement(RootLayout.XamlRoot);
                if (focusEle is TextBox)
                {
                    return;
                }
            }

            e.Handled = RootLayout.TryTogglePlayPauseIfInPlayer();
        }
        else if (e.Key == VirtualKey.Right)
        {
            object focusEle = FocusManager.GetFocusedElement(RootLayout.XamlRoot);
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
            object focusEle = FocusManager.GetFocusedElement(RootLayout.XamlRoot);
            if (focusEle is TextBox)
            {
                return;
            }

            e.Handled = RootLayout.TryCancelRightArrow();
        }
    }

    private RectInt32 GetRenderRect(RectInt32 workArea)
    {
        double scaleFactor = this.GetDpiForWindow() / 96d;
        double previousWidth = _settings.MainWindowWidth;
        double previousHeight = _settings.MainWindowHeight;
        int width = Convert.ToInt32(previousWidth * scaleFactor);
        int height = Convert.ToInt32(previousHeight * scaleFactor);

        // Ensure the window is not larger than the work area.
        if (height > workArea.Height - 20)
        {
            height = workArea.Height - 20;
        }

        PointInt32 lastPoint = GetSavedWindowPosition();
        bool isZeroPoint = lastPoint is
        {
            X: 0,
            Y: 0
        };
        bool isValidPosition = lastPoint.X >= workArea.X && lastPoint.Y >= workArea.Y;
        double left = isZeroPoint || !isValidPosition ? (workArea.Width - width) / 2d : lastPoint.X;
        double top = isZeroPoint || !isValidPosition ? (workArea.Height - height) / 2d : lastPoint.Y;
        return new RectInt32(Convert.ToInt32(left), Convert.ToInt32(top), width, height);
    }

    private void MoveAndResize()
    {
        PointInt32 lastPoint = GetSavedWindowPosition();
        DisplayArea displayArea =
            DisplayArea.GetFromPoint(lastPoint, DisplayAreaFallback.Primary) ?? DisplayArea.Primary;
        RectInt32 rect = GetRenderRect(displayArea.WorkArea);
        AppWindow.MoveAndResize(rect);
    }

    private void SaveCurrentWindowStats()
    {
        int left = AppWindow.Position.X;
        int top = AppWindow.Position.Y;
        BOOL isMaximized = PInvoke.IsZoomed(new HWND(this.GetWindowHandle()));
        _settings.IsMainWindowMaximized = isMaximized;

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
        PointerPoint point = e.GetCurrentPoint((UIElement)sender);
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
        using Process curProcess = Process.GetCurrentProcess();
        using ProcessModule? curModule = curProcess.MainModule;
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
            int vkCode = Marshal.ReadInt32(lParam);
            bool isCtrlPressed = (PInvoke.GetKeyState((int)VirtualKey.Control) & 0x8000) != 0;
            PlayerKeyboardEventArgs args = new(vkCode, isCtrlPressed);
            KeyDown?.Invoke(null, args);
            if (args.Handled)
            {
                return new LRESULT(1);
            }
        }
        else if (nCode >= 0 && wParam.Value == WM_KEYUP)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            bool isCtrlPressed = (PInvoke.GetKeyState((int)VirtualKey.Control) & 0x8000) != 0;
            PlayerKeyboardEventArgs args = new(vkCode, isCtrlPressed);
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
