using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using PhoenixReadCopilot.Contracts.Services;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Helpers;
using ZLogger;

namespace PhoenixReadCopilot.Services;

/// <summary>
/// 应用主题选择与多窗口同步服务实现.
/// </summary>
internal sealed class ThemeSelectorService : IThemeSelectorService, IDisposable
{
    private readonly ISettingsService _settingsService;
    private readonly IWindowService _windowService;
    private readonly ILogger<ThemeSelectorService> _logger;
    private readonly HashSet<Window> _subscribedWindows = [];
    private bool _isInitialized;
    private bool _lastIsDarkTheme;

    public AppTheme Theme => _settingsService.Settings.AppTheme;

    public ElementTheme RequestedElementTheme => ThemeHelper.ToElementTheme(Theme);

    public bool IsDarkTheme
    {
        get
        {
            if (
                _windowService.Windows.FirstOrDefault(w => w.Content is FrameworkElement)
                is { Content: FrameworkElement root }
            )
            {
                return root.ActualTheme == ElementTheme.Dark;
            }

            return RequestedElementTheme switch
            {
                ElementTheme.Dark => true,
                ElementTheme.Light => false,
                _ => Application.Current?.RequestedTheme == ApplicationTheme.Dark,
            };
        }
    }

    public event EventHandler<bool>? ThemeChanged;

    public ThemeSelectorService(
        ISettingsService settingsService,
        IWindowService windowService,
        ILogger<ThemeSelectorService> logger
    )
    {
        _settingsService = settingsService;
        _windowService = windowService;
        _logger = logger;
    }

    public void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
        _lastIsDarkTheme = IsDarkTheme;
        _windowService.WindowRegistered += OnWindowRegistered;
        _windowService.WindowUnregistered += OnWindowUnregistered;

        foreach (Window window in _windowService.Windows.ToList())
        {
            ApplyTheme(window);
        }
    }

    public void SetTheme(AppTheme theme)
    {
        if (_settingsService.Settings.AppTheme != theme)
        {
            _settingsService.Settings.AppTheme = theme;
        }

        ElementTheme elementTheme = ThemeHelper.ToElementTheme(theme);
        _windowService.ChangeTheme(elementTheme);
        NotifyIfEffectiveThemeChanged();
    }

    public void ApplyTheme(Window window)
    {
        if (window.Content is not FrameworkElement root)
        {
            return;
        }

        if (_subscribedWindows.Add(window))
        {
            root.ActualThemeChanged += OnWindowActualThemeChanged;
        }

        ElementTheme targetTheme = RequestedElementTheme;
        if (root.RequestedTheme != targetTheme)
        {
            root.RequestedTheme = targetTheme;
        }
    }

    private void OnWindowRegistered(object? sender, Window window)
    {
        ApplyTheme(window);
    }

    private void OnWindowUnregistered(object? sender, Window window)
    {
        if (_subscribedWindows.Remove(window) && window.Content is FrameworkElement root)
        {
            root.ActualThemeChanged -= OnWindowActualThemeChanged;
        }
    }

    private void OnWindowActualThemeChanged(FrameworkElement sender, object args)
    {
        NotifyIfEffectiveThemeChanged();
    }

    private void NotifyIfEffectiveThemeChanged()
    {
        bool currentIsDark = IsDarkTheme;
        if (currentIsDark != _lastIsDarkTheme)
        {
            _lastIsDarkTheme = currentIsDark;
            _logger.ZLogInformation($"[ThemeSelectorService] Effective theme changed: IsDark={currentIsDark}");
            ThemeChanged?.Invoke(this, currentIsDark);
        }
    }

    public void Dispose()
    {
        _windowService.WindowRegistered -= OnWindowRegistered;
        _windowService.WindowUnregistered -= OnWindowUnregistered;

        foreach (Window window in _subscribedWindows)
        {
            if (window.Content is FrameworkElement root)
            {
                root.ActualThemeChanged -= OnWindowActualThemeChanged;
            }
        }

        _subscribedWindows.Clear();
    }
}
