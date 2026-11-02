using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using PhoenixReadCopilot.UI.Models.Constants;
using PhoenixReadCopilot.UI.Models.Settings;
using PhoenixReadCopilot.UI.Strings;
using PhoenixReadCopilot.UI.ViewModels.Core;
using WinUIKernel.Share.Toolkits;

namespace PhoenixReadCopilot.UI.ViewModels.View;

/// <summary>
/// 设置页面视图模型.
/// </summary>
public sealed partial class SettingsPageViewModel
{
    private readonly AppViewModel _appViewModel;
    private readonly NavigationViewModel _navigationViewModel;
    private readonly IAppToolkit _appToolkit;
    private readonly IFileToolkit _fileToolkit;
    public AppSettings Settings;
    private bool _isInitialized;

    /// <summary>
    /// 当前应用主题.
    /// </summary>
    public ElementTheme AppTheme => Settings.AppTheme;

    /// <summary>
    /// 主题显示文本.
    /// </summary>
    public string AppThemeText =>
        AppTheme switch
        {
            ElementTheme.Light => Resources.LightTheme,
            ElementTheme.Dark => Resources.DarkTheme,
            _ => Resources.SystemDefault,
        };

    /// <summary>
    /// 是否显示顶部导航栏.
    /// </summary>
    public bool IsTopNavShown => Settings.IsTopNavBarShown;

    [ObservableProperty]
    public partial string PackageVersion { get; set; }

    [ObservableProperty]
    public partial string Copyright { get; set; }

    [ObservableProperty]
    public partial List<PlayerDisplayMode> PlayerDisplayModeCollection { get; set; }

    [ObservableProperty]
    public partial List<PreferCodecType> PreferCodecCollection { get; set; }

    [ObservableProperty]
    public partial List<PreferQualityType> PreferQualityCollection { get; set; }

    [ObservableProperty]
    public partial List<PreferDecodeType> PreferDecodeCollection { get; set; }

    [ObservableProperty]
    public partial List<PlayerType> PlayerTypeCollection { get; set; }

    [ObservableProperty]
    public partial List<MTCBehavior> MTCBehaviorCollection { get; set; }
}
