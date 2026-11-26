using Microsoft.Extensions.Logging;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using PhoenixReadCopilot.Contracts.Services;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Core.Helpers;
using PhoenixReadCopilot.Core.Models.Settings;
using PhoenixReadCopilot.Helpers;
using Windows.UI;
using WinRT;
using WinUI3Package;
using WinUIEx;
using ZLogger;
using XamlSystemBackdrop = Microsoft.UI.Xaml.Media.SystemBackdrop;

namespace PhoenixReadCopilot.Services;

/// <summary>
/// 窗口背景材质管理服务实现（支持多窗口同步、失焦停更、透明度与着色颜色控制）.
/// </summary>
internal sealed class MaterialSelectorService : IMaterialSelectorService
{
    private sealed class WindowBackdropSession(
        Window window,
        ICompositionSupportsSystemBackdrop target,
        SystemBackdropConfiguration configuration
    )
    {
        public Window Window { get; } = window;
        public ICompositionSupportsSystemBackdrop Target { get; } = target;
        public SystemBackdropConfiguration Configuration { get; } = configuration;
        public ISystemBackdropControllerWithTargets? Controller { get; set; }
        public XamlSystemBackdrop? CustomBackdrop { get; set; }
        public bool IsActivated { get; set; } = true;
    }

    private static readonly TimeSpan AppearanceDebounceDelay = TimeSpan.FromMilliseconds(100);

    private readonly ISettingsService _settingsService;
    private readonly IWindowService _windowService;
    private readonly IThemeSelectorService _themeSelectorService;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly ILogger<MaterialSelectorService> _logger;
    private readonly Dictionary<Window, WindowBackdropSession> _sessions = [];
    private readonly Debouncer _appearanceDebouncer;

    private bool _isInitialized;
    private bool _disposed;

    private AppSettings Settings => _settingsService.Settings;

    public BackdropMaterialType Material => Settings.BackdropMaterial;

    public bool IsFallbackEnabled => Settings.IsBackdropFallbackEnabled;

    public byte LuminosityOpacity => Settings.BackdropLuminosityOpacity;

    public Color TintColor => ColorHelper.FromArgbUInt(Settings.BackdropTintColor);

    public bool SupportsFallback => IsControllerMaterial(Material) || ShouldUseTenMica(Material);

    public bool SupportsLuminosityOpacity =>
        (IsControllerMaterial(Material) && !ShouldUseTenMica(Material))
        || Material == BackdropMaterialType.Transparent;

    public bool SupportsTintColor =>
        (IsControllerMaterial(Material) && !ShouldUseTenMica(Material))
        || Material == BackdropMaterialType.Pure
        || Material == BackdropMaterialType.Transparent;

    public event EventHandler? AppearanceChanged;

    public MaterialSelectorService(
        ISettingsService settingsService,
        IWindowService windowService,
        IThemeSelectorService themeSelectorService,
        DispatcherQueue dispatcherQueue,
        ILogger<MaterialSelectorService> logger
    )
    {
        _settingsService = settingsService;
        _windowService = windowService;
        _themeSelectorService = themeSelectorService;
        _dispatcherQueue = dispatcherQueue;
        _logger = logger;
        _appearanceDebouncer = new Debouncer(AppearanceDebounceDelay, ex =>
            _logger.ZLogError(ex, $"[MaterialSelectorService] Appearance debounce failed.")
        );
    }

    public void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
        _windowService.WindowRegistered += OnWindowRegistered;
        _windowService.WindowUnregistered += OnWindowUnregistered;
        _themeSelectorService.ThemeChanged += OnThemeChanged;

        if (!Settings.UseCustomBackdropAppearance)
        {
            ApplyDefaultAppearanceToSettings(Material, _themeSelectorService.IsDarkTheme);
        }

        foreach (Window window in _windowService.Windows.ToList())
        {
            AttachWindow(window);
        }

        AppearanceChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetMaterial(BackdropMaterialType material, bool force = false)
    {
        if (Material == material && !force)
        {
            return;
        }

        _appearanceDebouncer.Cancel();
        Settings.BackdropMaterial = material;
        Settings.UseCustomBackdropAppearance = false;
        ApplyDefaultAppearanceToSettings(material, _themeSelectorService.IsDarkTheme);

        foreach (WindowBackdropSession session in _sessions.Values)
        {
            ApplyMaterialToSession(session);
        }

        AppearanceChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetFallbackEnabled(bool enabled)
    {
        if (Settings.IsBackdropFallbackEnabled == enabled)
        {
            return;
        }

        Settings.IsBackdropFallbackEnabled = enabled;

        foreach (WindowBackdropSession session in _sessions.Values)
        {
            UpdateSessionConfiguration(session);
        }

        AppearanceChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetLuminosityOpacity(byte opacity)
    {
        byte clamped = Math.Clamp(opacity, (byte)0, (byte)100);
        if (Settings.BackdropLuminosityOpacity == clamped && Settings.UseCustomBackdropAppearance)
        {
            return;
        }

        Settings.UseCustomBackdropAppearance = true;
        Settings.BackdropLuminosityOpacity = clamped;
        ScheduleAppearanceUpdate();
    }

    public void SetTintColor(Color color)
    {
        uint argb = ColorHelper.ToArgbUInt(color);
        if (Settings.BackdropTintColor == argb && Settings.UseCustomBackdropAppearance)
        {
            return;
        }

        Settings.UseCustomBackdropAppearance = true;
        Settings.BackdropTintColor = argb;
        ScheduleAppearanceUpdate();
    }

    public void ResetMaterial()
    {
        _appearanceDebouncer.Cancel();
        Settings.IsBackdropFallbackEnabled = true;
        Settings.UseCustomBackdropAppearance = false;
        SetMaterial(BackdropMaterialType.DesktopAcrylic, force: true);

        foreach (WindowBackdropSession session in _sessions.Values)
        {
            UpdateSessionConfiguration(session);
        }

        AppearanceChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ScheduleAppearanceUpdate()
    {
        AppearanceChanged?.Invoke(this, EventArgs.Empty);

        _appearanceDebouncer.Debounce(() =>
        {
            if (_disposed)
            {
                return;
            }

            _dispatcherQueue.TryEnqueue(ApplyAppearanceToAllSessions);
        });
    }

    private void ApplyAppearanceToAllSessions()
    {
        if (_disposed)
        {
            return;
        }

        foreach (WindowBackdropSession session in _sessions.Values)
        {
            ApplyAppearanceToSession(session);
        }
    }

    private void AttachWindow(Window window)
    {
        if (_sessions.ContainsKey(window))
        {
            return;
        }

        ICompositionSupportsSystemBackdrop target = window.As<ICompositionSupportsSystemBackdrop>();
        SystemBackdropConfiguration configuration = new()
        {
            Theme = ToSystemBackdropTheme(_themeSelectorService.RequestedElementTheme),
            IsInputActive = true,
        };

        WindowBackdropSession session = new(window, target, configuration);
        _sessions[window] = session;

        window.Activated += OnWindowActivated;
        if (window.Content is FrameworkElement root)
        {
            root.ActualThemeChanged += OnWindowRootActualThemeChanged;
        }

        ApplyMaterialToSession(session);
    }

    private void DetachWindow(Window window)
    {
        if (!_sessions.Remove(window, out WindowBackdropSession? session))
        {
            return;
        }

        window.Activated -= OnWindowActivated;
        if (window.Content is FrameworkElement root)
        {
            root.ActualThemeChanged -= OnWindowRootActualThemeChanged;
        }

        ClearSessionBackdrop(session);
    }

    private void ApplyMaterialToSession(WindowBackdropSession session)
    {
        ClearSessionBackdrop(session);

        try
        {
            BackdropMaterialType effectiveMaterial = ResolveSupportedMaterial(Material);
            ISystemBackdropControllerWithTargets? controller = ShouldUseTenMica(effectiveMaterial)
                ? null
                : CreateController(effectiveMaterial);
            if (controller is not null)
            {
                session.Controller = controller;
                UpdateSessionConfiguration(session);
                ApplyAppearanceToSession(session);
                controller.AddSystemBackdropTarget(session.Target);
                controller.SetSystemBackdropConfiguration(session.Configuration);
                return;
            }

            XamlSystemBackdrop customBackdrop = CreateCustomBackdrop(
                effectiveMaterial,
                session.Window
            );
            session.CustomBackdrop = customBackdrop;
            session.Window.SystemBackdrop = customBackdrop;
        }
        catch (Exception ex)
        {
            _logger.ZLogError(
                ex,
                $"[MaterialSelectorService] Failed to apply material {Material}."
            );
            ApplyPureFallback(session);
        }
    }

    private void ApplyPureFallback(WindowBackdropSession session)
    {
        try
        {
            ClearSessionBackdrop(session);
            SolidColorBackdrop fallback = new(GetOpaqueTintColor());
            session.CustomBackdrop = fallback;
            session.Window.SystemBackdrop = fallback;
        }
        catch (Exception ex)
        {
            _logger.ZLogError(
                ex,
                $"[MaterialSelectorService] Failed to apply pure fallback backdrop."
            );
        }
    }

    private void ClearSessionBackdrop(WindowBackdropSession session)
    {
        if (session.Controller is not null)
        {
            try
            {
                session.Controller.RemoveAllSystemBackdropTargets();
                session.Controller.Dispose();
            }
            catch (Exception ex)
            {
                _logger.ZLogWarning(
                    ex,
                    $"[MaterialSelectorService] Failed to dispose backdrop controller."
                );
            }
            finally
            {
                session.Controller = null;
            }
        }

        if (session.CustomBackdrop is not null || session.Window.SystemBackdrop is not null)
        {
            session.Window.SystemBackdrop = null;
            session.CustomBackdrop = null;
        }
    }

    private void ApplyAppearanceToSession(WindowBackdropSession session)
    {
        float luminosityOpacity = LuminosityOpacity / 100f;
        Color tintColor = TintColor;
        Color fallbackColor = GetOpaqueTintColor();

        if (session.Controller is MicaController micaController)
        {
            micaController.LuminosityOpacity = luminosityOpacity;
            micaController.TintColor = tintColor;
            micaController.FallbackColor = fallbackColor;
        }
        else if (session.Controller is DesktopAcrylicController acrylicController)
        {
            acrylicController.LuminosityOpacity = luminosityOpacity;
            acrylicController.TintColor = tintColor;
            acrylicController.FallbackColor = fallbackColor;
        }
        else if (session.CustomBackdrop is SolidColorBackdrop solidBackdrop)
        {
            solidBackdrop.UpdateColor(GetOpaqueTintColor());
        }
        else if (session.CustomBackdrop is TransparentTintBackdrop transparentBackdrop)
        {
            transparentBackdrop.TintColor = GetTransparentTintColor(tintColor, LuminosityOpacity);
        }
        else if (session.CustomBackdrop is TenMicaBackdrop tenMicaBackdrop)
        {
            tenMicaBackdrop.LuminosityOpacity = luminosityOpacity;
            tenMicaBackdrop.TintColor = tintColor;
            tenMicaBackdrop.TintOpacity = GetTenMicaTintOpacity(
                Material,
                _themeSelectorService.IsDarkTheme
            );
            tenMicaBackdrop.FallbackColor = fallbackColor;
        }
    }

    private void UpdateSessionConfiguration(WindowBackdropSession session)
    {
        session.Configuration.Theme = GetSessionSystemBackdropTheme(session.Window);
        session.Configuration.IsInputActive = !IsFallbackEnabled || session.IsActivated;
        session.Controller?.SetSystemBackdropConfiguration(session.Configuration);

        if (session.CustomBackdrop is TenMicaBackdrop tenMicaBackdrop)
        {
            tenMicaBackdrop.EnableWhenInactive = !IsFallbackEnabled;
        }
    }

    private BackdropMaterialType ResolveSupportedMaterial(BackdropMaterialType material)
    {
        if (material == BackdropMaterialType.Mica)
        {
            if (IsWindows10)
            {
                return material;
            }

            if (!MicaController.IsSupported())
            {
                _logger.ZLogWarning(
                    $"[MaterialSelectorService] Mica is not supported on this system, falling back to Pure."
                );
                return BackdropMaterialType.Pure;
            }
        }
        else if (material == BackdropMaterialType.MicaAlt)
        {
            // Win10 下支持使用原生 MicaController，非 Win10 系统依然校验系统支持性
            if (!IsWindows10 && !MicaController.IsSupported())
            {
                _logger.ZLogWarning(
                    $"[MaterialSelectorService] MicaAlt is not supported on this system, falling back to Pure."
                );
                return BackdropMaterialType.Pure;
            }
        }

        if (
            material
                is BackdropMaterialType.DesktopAcrylic
                    or BackdropMaterialType.AcrylicBase
                    or BackdropMaterialType.AcrylicThin
            && !DesktopAcrylicController.IsSupported()
        )
        {
            _logger.ZLogWarning(
                $"[MaterialSelectorService] DesktopAcrylic is not supported on this system, falling back to Pure."
            );
            return BackdropMaterialType.Pure;
        }

        return material;
    }

    private static bool IsWindows10 => !OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000);

    private static bool ShouldUseTenMica(BackdropMaterialType material) =>
        IsWindows10 && material == BackdropMaterialType.Mica;

    private static float GetTenMicaTintOpacity(BackdropMaterialType material, bool isDarkTheme) =>
        isDarkTheme ? 0.8f : 0.5f;

    private static ISystemBackdropControllerWithTargets? CreateController(
        BackdropMaterialType material
    ) =>
        material switch
        {
            BackdropMaterialType.Mica => new MicaController { Kind = MicaKind.Base },
            BackdropMaterialType.MicaAlt => new MicaController { Kind = MicaKind.BaseAlt },
            BackdropMaterialType.DesktopAcrylic => new DesktopAcrylicController
            {
                Kind = DesktopAcrylicKind.Default,
            },
            BackdropMaterialType.AcrylicBase => new DesktopAcrylicController
            {
                Kind = DesktopAcrylicKind.Base,
            },
            BackdropMaterialType.AcrylicThin => new DesktopAcrylicController
            {
                Kind = DesktopAcrylicKind.Thin,
            },
            _ => null,
        };

    private XamlSystemBackdrop CreateCustomBackdrop(BackdropMaterialType material, Window window)
    {
        if (ShouldUseTenMica(material))
        {
            TenMicaBackdrop tenMica = new()
            {
                EnableWhenInactive = !IsFallbackEnabled,
                LuminosityOpacity = LuminosityOpacity / 100f,
                TintColor = TintColor,
                TintOpacity = GetTenMicaTintOpacity(material, _themeSelectorService.IsDarkTheme),
                FallbackColor = GetOpaqueTintColor(),
            };
            if (window.Content is FrameworkElement root)
            {
                tenMica.BindThemeTo = root;
            }

            return tenMica;
        }

        return material switch
        {
            BackdropMaterialType.Blur => new BlurredBackdrop(),
            BackdropMaterialType.Transparent => new TransparentTintBackdrop
            {
                TintColor = GetTransparentTintColor(TintColor, LuminosityOpacity),
            },
            BackdropMaterialType.Animated => new ColorAnimatedBackdrop(),
            _ => new SolidColorBackdrop(GetOpaqueTintColor()),
        };
    }

    private Color GetOpaqueTintColor()
    {
        Color tint = TintColor;
        return Color.FromArgb(255, tint.R, tint.G, tint.B);
    }

    private static Color GetTransparentTintColor(Color tintColor, byte opacity)
    {
        byte alpha = (byte)Math.Round(tintColor.A * (opacity / 100.0));
        return Color.FromArgb(alpha, tintColor.R, tintColor.G, tintColor.B);
    }

    private static bool IsControllerMaterial(BackdropMaterialType material) =>
        material
            is BackdropMaterialType.Mica
                or BackdropMaterialType.MicaAlt
                or BackdropMaterialType.DesktopAcrylic
                or BackdropMaterialType.AcrylicBase
                or BackdropMaterialType.AcrylicThin;

    private SystemBackdropTheme GetSessionSystemBackdropTheme(Window window)
    {
        if (window.Content is FrameworkElement root)
        {
            return ToSystemBackdropTheme(root.ActualTheme);
        }

        return ToSystemBackdropTheme(_themeSelectorService.RequestedElementTheme);
    }

    private static SystemBackdropTheme ToSystemBackdropTheme(ElementTheme theme) =>
        theme switch
        {
            ElementTheme.Light => SystemBackdropTheme.Light,
            ElementTheme.Dark => SystemBackdropTheme.Dark,
            _ => SystemBackdropTheme.Default,
        };

    private void ApplyDefaultAppearanceToSettings(BackdropMaterialType material, bool isDarkTheme)
    {
        (byte opacity, uint colorArgb) = GetDefaultAppearance(material, isDarkTheme);
        Settings.BackdropLuminosityOpacity = opacity;
        Settings.BackdropTintColor = colorArgb;
    }

    private static (byte Opacity, uint ColorArgb) GetDefaultAppearance(
        BackdropMaterialType material,
        bool isDarkTheme
    )
    {
        if (isDarkTheme)
        {
            return material switch
            {
                BackdropMaterialType.Pure => (100, 0xFF202020),
                BackdropMaterialType.Mica => (100, 0xFF202020),
                BackdropMaterialType.MicaAlt => (100, 0xFF0A0A0A),
                BackdropMaterialType.DesktopAcrylic => (85, 0xFF2C2C2C),
                BackdropMaterialType.AcrylicBase => (85, 0xFF202020),
                BackdropMaterialType.AcrylicThin => (45, 0xFF545454),
                BackdropMaterialType.Transparent => (40, 0xFF202020),
                _ => (85, 0xFF2C2C2C),
            };
        }

        return material switch
        {
            BackdropMaterialType.Pure => (100, 0xFFF3F3F3),
            BackdropMaterialType.Mica => (100, 0xFFF3F3F3),
            BackdropMaterialType.MicaAlt => (100, 0xFFDADADA),
            BackdropMaterialType.DesktopAcrylic => (85, 0xFFFCFCFC),
            BackdropMaterialType.AcrylicBase => (85, 0xFFF3F3F3),
            BackdropMaterialType.AcrylicThin => (45, 0xFFD3D3D3),
            BackdropMaterialType.Transparent => (40, 0xFFFFFFFF),
            _ => (85, 0xFFFCFCFC),
        };
    }

    private void OnWindowRegistered(object? sender, Window window)
    {
        AttachWindow(window);
    }

    private void OnWindowUnregistered(object? sender, Window window)
    {
        DetachWindow(window);
    }

    private void OnWindowActivated(object sender, WindowActivatedEventArgs args)
    {
        if (
            sender is not Window window
            || !_sessions.TryGetValue(window, out WindowBackdropSession? session)
        )
        {
            return;
        }

        session.IsActivated = args.WindowActivationState != WindowActivationState.Deactivated;
        if (IsFallbackEnabled)
        {
            UpdateSessionConfiguration(session);
        }
    }

    private void OnWindowRootActualThemeChanged(FrameworkElement sender, object args)
    {
        foreach (WindowBackdropSession session in _sessions.Values)
        {
            if (ReferenceEquals(session.Window.Content, sender))
            {
                UpdateSessionConfiguration(session);
            }
        }
    }

    private void OnThemeChanged(object? sender, bool isDarkTheme)
    {
        if (!Settings.UseCustomBackdropAppearance)
        {
            ApplyDefaultAppearanceToSettings(Material, isDarkTheme);
        }

        foreach (WindowBackdropSession session in _sessions.Values)
        {
            UpdateSessionConfiguration(session);
            ApplyAppearanceToSession(session);
        }

        AppearanceChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _appearanceDebouncer.Dispose();
        _windowService.WindowRegistered -= OnWindowRegistered;
        _windowService.WindowUnregistered -= OnWindowUnregistered;
        _themeSelectorService.ThemeChanged -= OnThemeChanged;

        foreach (Window window in _sessions.Keys.ToList())
        {
            DetachWindow(window);
        }
    }
}
